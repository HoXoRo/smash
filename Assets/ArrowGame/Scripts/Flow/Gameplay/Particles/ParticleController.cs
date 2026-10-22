using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Service;
using UnityEngine;

namespace Gameplay.Particles
{
    public class ParticleController : ServiceMonoBehaviour
    {
        private ParticleData _data;
        private readonly Dictionary<ParticleType, ParticlePlayer> _cache = new Dictionary<ParticleType, ParticlePlayer>();
        private readonly Dictionary<ParticleType, UniTaskCompletionSource<ParticlePlayer>> _loads = new Dictionary<ParticleType, UniTaskCompletionSource<ParticlePlayer>>();
        private readonly List<GameObject> _loadedAssets = new List<GameObject>();
        private bool _disposed;
        private int _pendingLoads;

        public void Initialize(ParticleData data) { _data = data; }

        private async UniTask<ParticlePlayer> SpawnAsync(ParticleType type)
        {
            _pendingLoads++;
            try
            {
                var item = _data?.items?.FirstOrDefault(x => x.type == type);
                if (item == null) return null;
                GameObject prefab = item.prefab;
                if (prefab == null && !string.IsNullOrEmpty(item.prefabAssetName))
                {
                    prefab = await GF.Resource.LoadAssetAwait<GameObject>(item.prefabAssetName);
                    if (_disposed)
                    {
                        if (prefab != null) GF.Resource.UnloadAsset(prefab);
                        return null;
                    }
                    if (prefab != null) _loadedAssets.Add(prefab);
                }
                if (_disposed || prefab == null) return null;
                var instance = Instantiate(prefab, transform);
                var player = instance.GetComponent<ParticlePlayer>();
                if (player == null)
                {
                    Destroy(instance);
                    throw new InvalidOperationException("Particle prefab has no ParticlePlayer: " + prefab.name);
                }
                _cache[type] = player;
                return player;
            }
            finally { _pendingLoads--; }
        }

        public void PlayOnPosition(ParticleType type, Vector3 worldPos, Quaternion worldRotation, Vector3 spawnVelocity)
        {
            if (_disposed) return;
            if (_cache.TryGetValue(type, out var player) && player != null)
            {
                player.Play(type, worldPos, worldRotation, spawnVelocity);
                return;
            }
            PlayAsync(type, worldPos, worldRotation, spawnVelocity).Forget();
        }

        private async UniTask PlayAsync(ParticleType type, Vector3 pos, Quaternion rotation, Vector3 velocity)
        {
            try
            {
                if (!_loads.TryGetValue(type, out var source))
                {
                    // Publish a multi-waiter source before starting IO. Preserve() only
                    // caches a result; it cannot accept concurrent pending continuations.
                    source = new UniTaskCompletionSource<ParticlePlayer>();
                    _loads.Add(type, source);
                    CompleteSpawnAsync(type, source).Forget();
                }
                var player = await source.Task;
                if (!_disposed && player != null) player.Play(type, pos, rotation, velocity);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private async UniTask CompleteSpawnAsync(ParticleType type, UniTaskCompletionSource<ParticlePlayer> source)
        {
            try { source.TrySetResult(await SpawnAsync(type)); }
            catch (Exception exception)
            {
                // Remove before completing: continuations may immediately request a retry.
                if (_loads.TryGetValue(type, out var current) && ReferenceEquals(current, source))
                    _loads.Remove(type);
                source.TrySetException(exception);
            }
        }

        public bool IsAnyParticleObjectAlive()
        {
            return _pendingLoads > 0 || _cache.Any(x => x.Value != null && x.Value.AnyParticleObjectsAlive());
        }

        protected override void OnDestroy()
        {
            _disposed = true;
            foreach (var asset in _loadedAssets) GF.Resource.UnloadAsset(asset);
            _loadedAssets.Clear();
            _cache.Clear();
            _loads.Clear();
            base.OnDestroy();
        }
    }
}
