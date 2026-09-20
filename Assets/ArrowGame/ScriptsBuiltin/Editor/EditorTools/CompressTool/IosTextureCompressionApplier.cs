using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace UGF.EditorTools
{
    public static class IosTextureCompressionApplier
    {
        private const string ArrowGameRoot = "Assets/ArrowGame";

        [MenuItem("ArrowGame/iOS压缩/应用到 ArrowGame 全部资源", false, 100)]
        public static void ApplyToAllArrowGame()
        {
            if (!EditorUtility.DisplayDialog(
                    "iOS 压缩配置",
                    "将为 ArrowGame 下所有贴图与 SpriteAtlas 应用 iOS 默认压缩配置（ASTC）。\n是否继续？",
                    "继续",
                    "取消"))
                return;

            var texturePaths = CollectTexturePaths(ArrowGameRoot);
            var atlasPaths = CollectAtlasPaths(ArrowGameRoot);
            ApplyTextures(texturePaths);
            ApplyAtlases(atlasPaths);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[iOS压缩] 完成：贴图 {texturePaths.Count}，图集 {atlasPaths.Count}");
        }

        [MenuItem("ArrowGame/iOS压缩/应用到选中资源", false, 101)]
        public static void ApplyToSelection()
        {
            var texturePaths = new List<string>();
            var atlasPaths = new List<string>();

            foreach (var obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path))
                    continue;

                if (AssetDatabase.IsValidFolder(path))
                {
                    texturePaths.AddRange(CollectTexturePaths(path));
                    atlasPaths.AddRange(CollectAtlasPaths(path));
                    continue;
                }

                if (path.EndsWith(".spriteatlas") || path.EndsWith(".spriteatlasv2"))
                    atlasPaths.Add(path);
                else if (AssetImporter.GetAtPath(path) is TextureImporter)
                    texturePaths.Add(path);
            }

            ApplyTextures(texturePaths);
            ApplyAtlases(atlasPaths);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[iOS压缩] 选中资源处理完成：贴图 {texturePaths.Count}，图集 {atlasPaths.Count}");
        }

        public static void ApplyTextures(IList<string> assetPaths)
        {
            if (assetPaths == null || assetPaths.Count == 0)
                return;

            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < assetPaths.Count; i++)
                {
                    string path = assetPaths[i];
                    if (EditorUtility.DisplayCancelableProgressBar(
                            $"iOS 贴图压缩 ({i + 1}/{assetPaths.Count})",
                            path,
                            (i + 1f) / assetPaths.Count))
                        break;

                    ApplyTextureAtPath(path);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();
            }
        }

        public static void ApplyAtlases(IList<string> assetPaths)
        {
            if (assetPaths == null || assetPaths.Count == 0)
                return;

            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < assetPaths.Count; i++)
                {
                    string path = assetPaths[i];
                    if (EditorUtility.DisplayCancelableProgressBar(
                            $"iOS 图集压缩 ({i + 1}/{assetPaths.Count})",
                            path,
                            (i + 1f) / assetPaths.Count))
                        break;

                    ApplyAtlasAtPath(path);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();
            }
        }

        public static bool ApplyTextureAtPath(string assetPath)
        {
            var texImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (texImporter == null)
                return false;

            if (texImporter.textureType == TextureImporterType.NormalMap)
                return false;

            var category = IosTextureCompressionSettings.GetCategory(assetPath);
            bool hasAlpha = texImporter.DoesSourceTextureHaveAlpha();
            var iosSettings = IosTextureCompressionSettings.CreateIosPlatformSettings(category, hasAlpha);

            var texSettings = new TextureImporterSettings();
            texImporter.ReadTextureSettings(texSettings);
            texSettings.mipmapEnabled = false;
            texSettings.readable = false;
            if (texImporter.textureType == TextureImporterType.Sprite)
            {
                texSettings.alphaIsTransparency = true;
                texSettings.spriteMeshType = SpriteMeshType.FullRect;
            }

            texImporter.SetTextureSettings(texSettings);
            texImporter.SetPlatformTextureSettings(iosSettings);
            texImporter.SaveAndReimport();
            return true;
        }

        public static bool ApplyAtlasAtPath(string assetPath)
        {
#if !UNITY_2022_1_OR_NEWER
            Debug.LogWarning($"[iOS压缩] 当前 Unity 版本不支持 SpriteAtlasImporter: {assetPath}");
            return false;
#else
            var atlasImporter = AssetImporter.GetAtPath(assetPath) as SpriteAtlasImporter;
            if (atlasImporter == null)
                return false;

            var category = IosTextureCompressionSettings.GetCategory(assetPath);
            var iosSettings = IosTextureCompressionSettings.CreateIosPlatformSettings(category, hasAlpha: true);

            var textureSettings = atlasImporter.textureSettings;
            textureSettings.readable = false;
            textureSettings.generateMipMaps = false;
            textureSettings.sRGB = true;
            textureSettings.filterMode = FilterMode.Bilinear;
            // textureSettings.maxTextureSize = iosSettings.maxTextureSize;
            atlasImporter.textureSettings = textureSettings;

            var packingSettings = atlasImporter.packingSettings;
            packingSettings.enableRotation = false;
            packingSettings.enableTightPacking = true;
            packingSettings.enableAlphaDilation = true;
            packingSettings.padding = 4;
            atlasImporter.packingSettings = packingSettings;

            atlasImporter.SetPlatformSettings(iosSettings);
            atlasImporter.SaveAndReimport();
            return true;
#endif
        }

        private static List<string> CollectTexturePaths(string rootFolder)
        {
            var result = new List<string>();
            if (!AssetDatabase.IsValidFolder(rootFolder))
                return result;

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { rootFolder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".spriteatlas") || path.EndsWith(".spriteatlasv2"))
                    continue;
                if (AssetImporter.GetAtPath(path) is TextureImporter)
                    result.Add(path);
            }

            return result;
        }

        private static List<string> CollectAtlasPaths(string rootFolder)
        {
            var result = new List<string>();
            if (!Directory.Exists(Path.Combine(Directory.GetParent(Application.dataPath).FullName, rootFolder)))
                return result;

            string[] guids = AssetDatabase.FindAssets("t:SpriteAtlas", new[] { rootFolder });
            foreach (string guid in guids)
            {
                result.Add(AssetDatabase.GUIDToAssetPath(guid));
            }

            string[] v2Files = Directory.GetFiles(
                Path.Combine(Directory.GetParent(Application.dataPath).FullName, rootFolder),
                "*.spriteatlasv2",
                SearchOption.AllDirectories);
            foreach (string file in v2Files)
            {
                string assetPath = file.Replace('\\', '/');
                int assetsIndex = assetPath.IndexOf("Assets/");
                if (assetsIndex >= 0)
                {
                    assetPath = assetPath.Substring(assetsIndex);
                    if (!result.Contains(assetPath))
                        result.Add(assetPath);
                }
            }

            return result;
        }
    }
}
