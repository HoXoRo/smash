using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UGF.EditorTools.ResourceTools;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Editor.ResourceTools;
using UnityGameFramework.Runtime;

namespace UGF.EditorTools
{
    public class AssetBuildHandler : IBuildEventHandler
    {
        public bool ContinueOnFailure
        {
            get
            {
                return false;
            }
        }

        public void OnPreprocessPlatform(Platform platform, string workingPath, bool outputPackageSelected, string outputPackagePath, bool outputFullSelected, string outputFullPath, bool outputPackedSelected, string outputPackedPath)
        {

        }

        public void OnBuildAssetBundlesComplete(Platform platform, string workingPath, bool outputPackageSelected, string outputPackagePath, bool outputFullSelected, string outputFullPath, bool outputPackedSelected, string outputPackedPath, AssetBundleManifest assetBundleManifest)
        {

        }


        public void OnPostprocessPlatform(Platform platform, string workingPath, bool outputPackageSelected, string outputPackagePath, bool outputFullSelected, string outputFullPath, bool outputPackedSelected, string outputPackedPath, bool isSuccess)
        {
            //打包完成后把文件复制到StreamingAssets目录
            string srcAbPath = string.Empty;
            bool copyToStreamingAssets = false;
            if (outputPackageSelected)
            {
                srcAbPath = outputPackagePath;
                copyToStreamingAssets = true;
            }
            else if (outputPackedSelected)
            {
                srcAbPath = outputPackedPath;
                copyToStreamingAssets = true;
            }
            else if (outputFullSelected)
            {
                srcAbPath = outputFullPath;
            }
            if (string.IsNullOrEmpty(srcAbPath))
            {
                Debug.LogErrorFormat("AB资源目录为空.");
                return;
            }
            if (copyToStreamingAssets)
            {
                string[] fileNames = Directory.GetFiles(srcAbPath, "*", SearchOption.AllDirectories);
                string streamingAssetsPath = Application.streamingAssetsPath;
                foreach (string fileName in fileNames)
                {
                    var abAssetName = fileName.Substring(srcAbPath.Length);
                    string destFileName = Path.Combine(streamingAssetsPath, abAssetName);
                    FileInfo destFileInfo = new FileInfo(destFileName);
                    if (!destFileInfo.Directory.Exists)
                    {
                        destFileInfo.Directory.Create();
                    }
                    File.Copy(fileName, destFileName, true);
                }
                AssetDatabase.Refresh();
            }
        }

        public void OnPostprocessAllPlatforms(string productName, string companyName, string gameIdentifier, string gameFrameworkVersion, string unityVersion, string applicableGameVersion, int internalResourceVersion, BuildAssetBundleOptions buildAssetBundleOptions, bool zip, string outputDirectory, string workingPath, bool outputPackageSelected, string outputPackagePath, bool outputFullSelected, string outputFullPath, bool outputPackedSelected, string outputPackedPath, string buildReportPath)
        {

        }

        public void OnPreprocessAllPlatforms(string productName, string companyName, string gameIdentifier, string gameFrameworkVersion, string unityVersion, string applicableGameVersion, int internalResourceVersion, Platform platforms, AssetBundleCompressionType assetBundleCompression, string compressionHelperTypeName, bool additionalCompressionSelected, bool forceRebuildAssetBundleSelected, string buildEventHandlerTypeName, string outputDirectory, BuildAssetBundleOptions buildAssetBundleOptions, string workingPath, bool outputPackageSelected, string outputPackagePath, bool outputFullSelected, string outputFullPath, bool outputPackedSelected, string outputPackedPath, string buildReportPath)
        {
            RemoveStreamingAssetsBundles();
        }

        public void OnPostprocessAllPlatforms(string productName, string companyName, string gameIdentifier, string gameFrameworkVersion, string unityVersion, string applicableGameVersion, int internalResourceVersion, Platform platforms, AssetBundleCompressionType assetBundleCompression, string compressionHelperTypeName, bool additionalCompressionSelected, bool forceRebuildAssetBundleSelected, string buildEventHandlerTypeName, string outputDirectory, BuildAssetBundleOptions buildAssetBundleOptions, string workingPath, bool outputPackageSelected, string outputPackagePath, bool outputFullSelected, string outputFullPath, bool outputPackedSelected, string outputPackedPath, string buildReportPath)
        {

        }

        public void OnOutputUpdatableVersionListData(Platform platform, string versionListPath, int versionListLength, int versionListHashCode, int versionListCompressedLength, int versionListCompressedHashCode)
        {
            //将版本信息写入对应平台的version.json
            var dir = Path.GetDirectoryName(versionListPath);
            var resourceVersionStr = new DirectoryInfo(dir).Parent.Name.Split('_').Last();
            int resourceVersion = string.IsNullOrWhiteSpace(resourceVersionStr) ? 0 : int.Parse(resourceVersionStr);
            var outputVersionFile = UtilityBuiltin.AssetsPath.GetCombinePath(dir, ConstBuiltin.VersionFile);

            var outputVersionInfo = new VersionInfo()
            {
                ApplicableGameVersion = AppBuildSettings.Instance.ApplicableGameVersion,
                ForceUpdateApp = AppBuildSettings.Instance.ForceUpdateApp,
                AppUpdateDesc = AppBuildSettings.Instance.AppUpdateDesc,
                AppUpdateUrl = AppBuildSettings.Instance.AppUpdateUrl,
                UpdatePrefixUri = UtilityBuiltin.AssetsPath.GetCombinePath(AppBuildSettings.Instance.UpdatePrefixUri,Application.version, platform.ToString()),
                VersionListHashCode = versionListHashCode,
                VersionListLength = versionListLength,
                VersionListCompressedHashCode = versionListCompressedHashCode,
                VersionListCompressedLength = versionListCompressedLength,
                InternalResourceVersion = resourceVersion,
                LastAppVersion = Application.version
            };
            File.WriteAllText(outputVersionFile, UtilityBuiltin.Json.ToJson(outputVersionInfo));
        }


        [MenuItem("Game Framework/Resource Tools/Resolve Duplicate Assets【解决AB资源重复依赖冗余】", false, 100)]
        static void RefreshSharedAssets()
        {
            AutoResolveAbDuplicateAssets();
        }
        public static bool AutoResolveAbDuplicateAssets()
        {
            if (AppBuildSettings.Instance.UseResourceRule)
            {
                ResourceRuleEditorUtility.RefreshResourceCollection();
            }

            ResourceEditorController resEditor = new ResourceEditorController();
            if (resEditor.Load())
            {
                return ResolveDuplicateAssets(resEditor);
            }
            return false;
        }

        private static bool ResolveDuplicateAssets(ResourceEditorController resEditor)
        {
            // Stop traversal at explicitly assigned assets. Their dependencies belong
            // to their own bundle, not to every bundle which happens to reference them.
            var resources = resEditor.GetResources();
            var assigned = new HashSet<string>(resources.SelectMany(r => r.GetAssets()).Select(a => a.Name));
            var owners = new Dictionary<string, HashSet<string>>();
            var dependencyCache = new Dictionary<string, string[]>();
            foreach (var resource in resources)
            {
                var visited = new HashSet<string>();
                var pending = new Stack<string>();
                foreach (var asset in resource.GetAssets())
                    foreach (var dependency in GetDirectDependencies(asset.Name, dependencyCache))
                        pending.Push(dependency);
                while (pending.Count > 0)
                {
                    string path = pending.Pop();
                    if (!visited.Add(path) || assigned.Contains(path) || !CanShareAsset(path)) continue;
                    if (!owners.TryGetValue(path, out var users))
                        owners.Add(path, users = new HashSet<string>());
                    users.Add(resource.FullName);
                    foreach (var dependency in GetDirectDependencies(path, dependencyCache))
                        pending.Push(dependency);
                }
            }

            int sharedCount = 0;
            foreach (var pair in owners.OrderBy(p => p.Key, System.StringComparer.Ordinal))
            {
                if (pair.Value.Count < 2) continue;
                // Equal consumer sets share one bundle; unrelated UI and gameplay
                // dependencies do not accumulate in a single giant shared bundle.
                string consumers = string.Join("\n", pair.Value.OrderBy(x => x, System.StringComparer.Ordinal));
                string bundleName;
                using (var hash = System.Security.Cryptography.SHA256.Create())
                    bundleName = "AutoShared/" + System.BitConverter.ToString(
                        hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(consumers))).Replace("-", "").ToLowerInvariant();
                if (!resEditor.HasResource(bundleName, null) &&
                    !resEditor.AddResource(bundleName, null, null, LoadType.LoadFromMemoryAndDecrypt, true))
                {
                    Debug.LogError("Could not create shared bundle: " + bundleName);
                    return false;
                }
                if (!resEditor.AssignAsset(AssetDatabase.AssetPathToGUID(pair.Key), bundleName, null))
                {
                    Debug.LogError("Could not assign shared dependency: " + pair.Key);
                    return false;
                }
                sharedCount++;
            }
            // SourceAssetRootPath is ArrowGame, but shared shaders may live in plugins.
            // Remove only missing assets, not valid dependencies outside that editor filter.
            foreach (var asset in resEditor.GetResources().SelectMany(r => r.GetAssets()).ToArray())
                if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(asset.Guid)))
                    resEditor.UnassignAsset(asset.Guid);
            resEditor.RemoveUnusedResources();
            Debug.Log($"AB shared dependencies extracted: {sharedCount}");
            return resEditor.Save();
        }

        private static string[] GetDirectDependencies(string path, Dictionary<string, string[]> cache)
        {
            if (!cache.TryGetValue(path, out var dependencies))
            {
                dependencies = AssetDatabase.GetDependencies(path, false).Where(p => p != path).ToArray();
                cache.Add(path, dependencies);
            }
            return dependencies;
        }

        private static bool CanShareAsset(string path)
        {
            if (!path.StartsWith("Assets/", System.StringComparison.Ordinal) || AssetDatabase.IsValidFolder(path))
                return false;
            string extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension == ".cs" || extension == ".dll" || extension == ".asmdef" ||
                extension == ".asmref" || extension == ".unity") return false;
            return AssetDatabase.GetMainAssetTypeAtPath(path) != null;
        }

        /// <summary>
        /// 移除StreamingAssets目录的AB包
        /// </summary>
        internal static void RemoveStreamingAssetsBundles()
        {
            string streamingAssetsPath = Application.streamingAssetsPath;
            if (Directory.Exists(streamingAssetsPath))
            {
                var oldAbFiles = Directory.GetFiles(streamingAssetsPath, "*.dat", SearchOption.AllDirectories);
                var projectRoot = Directory.GetParent(Application.dataPath).FullName;
                foreach (var abFile in oldAbFiles)
                {
                    Debug.Log($"删除文件:{abFile}");
                    var relativePath = Path.GetRelativePath(projectRoot, abFile);
                    AssetDatabase.DeleteAsset(relativePath);
                }
                var dirInfo = new DirectoryInfo(streamingAssetsPath);
                var subDirs = dirInfo.GetDirectories("*", SearchOption.AllDirectories);
                foreach (var item in subDirs)
                {
                    if (!item.Exists) continue;
                    if (item.GetFiles("*", SearchOption.AllDirectories).Length <= 0)
                    {
                        Debug.Log($"删除文件夹:{item}");

                        var relativePath = Path.GetRelativePath(projectRoot, item.FullName);
                        AssetDatabase.DeleteAsset(relativePath);
                    }
                }
            }
        }
    }
}
