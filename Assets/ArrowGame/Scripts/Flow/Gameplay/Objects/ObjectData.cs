using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Level;
using UnityEngine;
using Util;

namespace Gameplay.Objects
{
    [CreateAssetMenu(fileName = "ObjectData", menuName = "Flow/Object Data", order = 0)]
    public class ObjectData : ScriptableObject
    {
        public List<ObjectDataItem> items;
        public GameObject tablePrefab;
        public GameObject blockerPrefab;
        public string tablePrefabAssetName;
        public string blockerPrefabAssetName;

        // Only the per-controller clone owns these resource references.
        [NonSerialized] private readonly Dictionary<string, GameObject> _loaded = new Dictionary<string, GameObject>();
        [NonSerialized] private bool _released;

        public GameObject Get(ObjectType type, FloatTriplet size)
        {
            var item = items?.FirstOrDefault(x => x.type == type && x.size == size);
            if (item == null) return null;
            if (item.prefab != null) return item.prefab;
            return !string.IsNullOrEmpty(item.prefabAssetName) && _loaded.TryGetValue(item.prefabAssetName, out var prefab) ? prefab : null;
        }

        public async UniTask LoadForLevelAsync(LevelData level, CancellationToken token)
        {
            var paths = new HashSet<string>();
            bool needsTable = false, needsBlocker = false;
            // Include every stage so later stage transitions remain synchronous.
            foreach (var stage in level.stages)
            {
                if (stage == null) continue;
                needsTable |= stage.tables != null && stage.tables.Count > 0;
                needsBlocker |= stage.blockers != null && stage.blockers.Count > 0;
                if (stage.objects == null) continue;
                foreach (var obj in stage.objects)
                {
                    if (obj == null) continue;
                    var item = items?.FirstOrDefault(x => x.type == obj.type && x.size == obj.size);
                    if (item == null)
                        throw new InvalidOperationException($"Missing object catalog entry: {obj.type} {obj.size}");
                    if (item.prefab == null) AddPath(paths, item.prefabAssetName);
                }
            }
            if (needsTable && tablePrefab == null) AddPath(paths, tablePrefabAssetName);
            if (needsBlocker && blockerPrefab == null) AddPath(paths, blockerPrefabAssetName);
            var tasks = new List<UniTask>();
            foreach (var path in paths)
                if (!_loaded.ContainsKey(path)) tasks.Add(LoadPrefabAsync(path, token));
            await UniTask.WhenAll(tasks).AttachExternalCancellation(token);
            token.ThrowIfCancellationRequested();
            if (needsTable && tablePrefab == null) tablePrefab = _loaded[tablePrefabAssetName];
            if (needsBlocker && blockerPrefab == null) blockerPrefab = _loaded[blockerPrefabAssetName];
            UnityGameFramework.Runtime.Log.Info($"[GameplayLoad] Level prefab assets: {paths.Count} (catalog: {items?.Count ?? 0})");
        }

        private static void AddPath(HashSet<string> paths, string path)
        {
            if (string.IsNullOrEmpty(path)) throw new InvalidOperationException("Gameplay prefab path is missing.");
            paths.Add(path);
        }

        private async UniTask LoadPrefabAsync(string path, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            // Resource IO cannot be cancelled. Release late arrivals after timeout/destruction.
            var prefab = await GF.Resource.LoadAssetAwait<GameObject>(path);
            if (_released || token.IsCancellationRequested)
            {
                if (prefab != null) GF.Resource.UnloadAsset(prefab);
                token.ThrowIfCancellationRequested();
                return;
            }
            if (prefab == null) throw new InvalidOperationException("Could not load gameplay prefab: " + path);
            _loaded.Add(path, prefab);
        }

        public void ReleaseLoadedPrefabs()
        {
            _released = true;
            foreach (var prefab in _loaded.Values) GF.Resource.UnloadAsset(prefab);
            _loaded.Clear();
            tablePrefab = null;
            blockerPrefab = null;
        }
    }
}
