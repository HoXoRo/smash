using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Gameplay.Objects;
using Gameplay.Particles;
using Level;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gameplay.Level
{
    // All access is on Unity's main thread. One UGF load per path, shared by leases.
    internal static class GameplayAssetCache
    {
        private sealed class Entry
        {
            internal string Path;
            internal Object Asset;
            internal int References;
            internal bool Common;
            internal bool Finished;
            internal readonly UniTaskCompletionSource<Object> Completion = new UniTaskCompletionSource<Object>();
        }

        internal sealed class Lease : IDisposable
        {
            private readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();
            private bool disposed;
            internal ObjectData Objects;
            internal ParticleData Particles;
            internal PhysicsMaterial Physics;

            internal async UniTask<T> Load<T>(string path, CancellationToken token, bool common = false, bool background = false)
                where T : Object
            {
                token.ThrowIfCancellationRequested();
                if (disposed) throw new ObjectDisposedException(nameof(Lease));
                if (string.IsNullOrEmpty(path)) throw new InvalidOperationException("Missing gameplay asset path. Rebuild the lightweight catalogs and ABs.");
                if (background)
                {
                    await UniTask.WaitUntil(() => BackgroundIsIdle(), cancellationToken: token);
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
                if (!entries.TryGetValue(path, out Entry entry))
                {
                    if (!cache.TryGetValue(path, out entry))
                    {
                        entry = new Entry { Path = path };
                        cache.Add(path, entry);
                        // Register ownership before starting a potentially synchronous editor load.
                        entry.References++;
                        entry.Common = common;
                        entries.Add(path, entry);
                        LoadEntry<T>(entry).Forget();
                    }
                    else
                    {
                        entry.References++;
                        entry.Common |= common;
                        entries.Add(path, entry);
                    }
                }
                Object asset = await entry.Completion.Task.AttachExternalCancellation(token);
                token.ThrowIfCancellationRequested();
                if (disposed) throw new ObjectDisposedException(nameof(Lease));
                if (!(asset is T typed)) throw new InvalidOperationException("Invalid gameplay asset: " + path);
                return typed;
            }

            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                if (Objects != null) Object.Destroy(Objects);
                if (Particles != null) Object.Destroy(Particles);
                foreach (Entry entry in entries.Values)
                {
                    entry.References--;
                    ReleaseUnused(entry);
                }
                entries.Clear();
            }
        }

        private static readonly Dictionary<string, Entry> cache = new Dictionary<string, Entry>();
        private static CancellationTokenSource preloadCancellation;
        private static Lease next;
        private static int preloadGeneration;
        private static Func<bool> backgroundIdle;
        private static bool BackgroundIsIdle() => Application.isFocused && !PlayLoadingUIForm.IsLoading
            && !Input.GetMouseButton(0) && (backgroundIdle?.Invoke() ?? false);
        private static readonly SemaphoreSlim preparationGate = new SemaphoreSlim(1, 1);
        private const string ObjectCatalog = "Assets/ArrowGame/ScriptableAssets/Flow/ObjectData.asset";
        private const string ParticleCatalog = "Assets/ArrowGame/ScriptableAssets/Flow/ParticleData.asset";
        private const string PhysicsPath = "Assets/ArrowGame/Gameplay/PhysicMaterial/PhysicsMat.physicMaterial";

        private static async UniTask LoadEntry<T>(Entry entry) where T : Object
        {
            try
            {
                entry.Asset = await GF.Resource.LoadAssetAwait<T>(entry.Path);
                if (entry.Asset == null) throw new InvalidOperationException("Missing gameplay asset: " + entry.Path);
                // CompletionSource supports simultaneous waiters, including a cancelled
                // preloader whose underlying Unity request is still running.
                entry.Completion.TrySetResult(entry.Asset);
            }
            catch (Exception exception)
            {
                entry.Completion.TrySetException(exception);
            }
            finally
            {
                entry.Finished = true;
                ReleaseUnused(entry);
            }
        }

        private static void ReleaseUnused(Entry entry)
        {
            if (!entry.Finished || entry.References != 0 || (entry.Common && entry.Asset != null)) return;
            if (cache.TryGetValue(entry.Path, out Entry current) && ReferenceEquals(entry, current)) cache.Remove(entry.Path);
            if (entry.Asset != null && GF.Resource != null) GF.Resource.UnloadAsset(entry.Asset);
            entry.Asset = null;
        }

        // Keep an already completed next-level lease until the foreground lease has
        // acquired its references. Cancel only background work, never shared Unity IO.
        internal static void CancelBackground()
        {
            preloadGeneration++;
            backgroundIdle = null;
            if (preloadCancellation == null) return;
            preloadCancellation.Cancel();
            preloadCancellation.Dispose();
            preloadCancellation = null;
        }

        internal static async UniTask<Lease> PrepareAsync(LevelData level, CancellationToken token)
        {
            CancelBackground();
            await preparationGate.WaitAsync(token);
            try { return await PrepareCore(level, token, false); }
            finally
            {
                next?.Dispose();
                next = null;
                preparationGate.Release();
            }
        }

        internal static void PreloadNext(int levelIndex, CancellationToken lifetime, Func<bool> isIdle)
        {
            CancelBackground();
            next?.Dispose();
            next = null;
            backgroundIdle = isIdle;
            preloadCancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime);
            Preload(levelIndex, preloadGeneration, preloadCancellation.Token).Forget();
        }

        private static async UniTask Preload(int levelIndex, int generation, CancellationToken token)
        {
            Lease prepared = null;
            bool entered = false;
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(2), cancellationToken: token);
                await UniTask.WaitUntil(() => BackgroundIsIdle(), cancellationToken: token);
                await preparationGate.WaitAsync(token);
                entered = true;
                LevelData level = await LevelLoader.GetLevelAsync(levelIndex).AttachExternalCancellation(token);
                if (level?.stages == null || level.stages.Count == 0) return;
                prepared = await PrepareCore(level, token, true);
                token.ThrowIfCancellationRequested();
                if (generation != preloadGeneration) return;
                next = prepared;
                prepared = null;
            }
            catch (OperationCanceledException) { }
            catch (Exception exception) { Debug.LogWarning("[GameplayAssets] Next-level preload skipped: " + exception.Message); }
            finally
            {
                prepared?.Dispose();
                if (entered) preparationGate.Release();
            }
        }

        private static async UniTask<Lease> PrepareCore(LevelData level, CancellationToken token, bool background)
        {
            var lease = new Lease();
            try
            {
                ObjectData catalog = await lease.Load<ObjectData>(ObjectCatalog, token, true, background);
                ParticleData effects = await lease.Load<ParticleData>(ParticleCatalog, token, true, background);
                if (catalog.items == null || effects.items == null) throw new InvalidOperationException("Invalid gameplay catalog.");
                lease.Objects = ScriptableObject.CreateInstance<ObjectData>();
                lease.Objects.items = new List<ObjectDataItem>();
                lease.Particles = ScriptableObject.CreateInstance<ParticleData>();
                lease.Particles.items = new List<ParticleDataItem>();
                lease.Physics = await lease.Load<PhysicsMaterial>(PhysicsPath, token, true, background);
                var wantedEffects = new HashSet<ParticleType> { ParticleType.BallGroundHit, ParticleType.ObjectGroundHit, ParticleType.BallObjectFirstHit, ParticleType.RocketExplode };
                var wantedObjects = new HashSet<(ObjectType, Util.FloatTriplet)>();
                if (level?.stages == null) throw new InvalidOperationException("Missing level stages.");
                foreach (LevelStageData stage in level.stages)
                {
                    if (stage?.objects == null) continue;
                    foreach (LevelObjectData item in stage.objects)
                        if (item != null) wantedObjects.Add((item.type, item.size));
                }
                foreach (var key in wantedObjects)
                {
                    ObjectDataItem item = catalog.items.FirstOrDefault(x => x.type == key.Item1 && x.size == key.Item2);
                    if (item == null) throw new InvalidOperationException($"No prefab indexed for object ID {(int)key.Item1}, size {key.Item2}.");
                    GameObject prefab = await lease.Load<GameObject>(item.prefabPath, token, false, background);
                    lease.Objects.items.Add(new ObjectDataItem { type = item.type, size = item.size, prefabPath = item.prefabPath, prefab = prefab });
                    CollectEffects(item.type, Mathf.RoundToInt(item.size.GetVolume()), wantedEffects);
                }
                if (level.stages.Any(s => s?.tables != null && s.tables.Count > 0))
                    lease.Objects.tablePrefab = await lease.Load<GameObject>(catalog.tablePrefabPath, token, true, background);
                if (level.stages.Any(s => s?.blockers != null && s.blockers.Count > 0))
                    lease.Objects.blockerPrefab = await lease.Load<GameObject>(catalog.blockerPrefabPath, token, true, background);
                foreach (ParticleType type in wantedEffects)
                {
                    ParticleDataItem item = effects.items.FirstOrDefault(x => x.type == type);
                    if (item == null) throw new InvalidOperationException("No particle indexed for particle ID " + (int)type);
                    bool common = type == ParticleType.BallGroundHit || type == ParticleType.ObjectGroundHit || type == ParticleType.BallObjectFirstHit || type == ParticleType.RocketExplode;
                    GameObject prefab = await lease.Load<GameObject>(item.prefabPath, token, common, background);
                    lease.Particles.items.Add(new ParticleDataItem { type = type, prefabPath = item.prefabPath, prefab = prefab });
                }
                Debug.Log($"[GameplayAssets] {(background ? "Preloaded" : "Prepared")} {wantedObjects.Count} object variants, {wantedEffects.Count} effects; cached assets: {cache.Count}.");
                return lease;
            }
            catch
            {
                lease.Dispose();
                throw;
            }
        }

        internal static void CollectEffects(ObjectType type, int volume, HashSet<ParticleType> effects)
        {
            if (ObjectParticleMaps.PlayParticles.TryGetValue((type, volume), out ParticleType hit)) effects.Add(hit);
            if (ObjectParticleMaps.TntExplodeParticles.TryGetValue((type, volume), out ParticleType explosion)) effects.Add(explosion);
            foreach (BreakableObject.BreakSource source in Enum.GetValues(typeof(BreakableObject.BreakSource)))
                if (BreakableObject.JamJarBreakParticles.TryGetValue((type, volume, source), out ParticleType jar)) effects.Add(jar);
            if (type == ObjectType.Wormhole)
            {
                effects.Add(ParticleType.WormholeHit);
                effects.Add(ParticleType.WormholeHitGround);
                effects.Add(ParticleType.WormholeHitTntExplode);
            }
            if (type == ObjectType.Tnt)
            {
                effects.Add(ParticleType.TntExplode);
                effects.Add(ParticleType.TntExplodeGround);
            }
        }
    }
}
