using System;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    /// <summary>
    /// 新图片首次导入时应用 Sprite 和 Android 压缩默认值。
    /// 已有 .meta 的资源（包括重新导入和移动资源）保留原设置。
    /// </summary>
    public sealed class AutoTextureImportProcessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            var importer = assetImporter as TextureImporter;
            if (importer == null || !importer.importSettingsMissing
                || !assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                return;

            // 保留 Preset 或其他导入工具指定的法线贴图等特殊类型。
            if (importer.textureType != TextureImporterType.Default
                && importer.textureType != TextureImporterType.Sprite)
                return;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            var androidSettings = importer.GetPlatformTextureSettings(
                AndroidTextureCompressionSettings.AndroidPlatformName);

            // 与现有压缩面板共用默认配置，固定使用截图的 1024 / ASTC 6x6。
            AndroidTextureCompressionSettings.ApplyCompressTexturePanelDefaults(
                settings, androidSettings);
            settings.textureShape = TextureImporterShape.Texture2D;
            settings.spritePixelsPerUnit = 100;
            settings.spriteExtrude = 1;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spritePivot = new Vector2(0.5f, 0.5f);
            settings.spriteGenerateFallbackPhysicsShape = true;
            settings.sRGBTexture = true;
            settings.alphaSource = TextureImporterAlphaSource.FromInput;
            settings.aniso = 1;

            androidSettings.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
            androidSettings.androidETC2FallbackOverride = AndroidETC2FallbackOverride.UseBuildSettings;
            androidSettings.crunchedCompression = false;

            // 预处理阶段直接写入，避免 SaveAndReimport 触发递归导入。
            importer.SetTextureSettings(settings);
            importer.SetPlatformTextureSettings(androidSettings);
        }
    }
}
