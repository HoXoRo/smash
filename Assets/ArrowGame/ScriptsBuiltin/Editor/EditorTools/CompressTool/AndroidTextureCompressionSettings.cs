using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    /// <summary>
    /// Android 贴图/图集默认压缩配置（ASTC + 分级 MaxSize）。
    /// </summary>
    public static class AndroidTextureCompressionSettings
    {
        public const string AndroidPlatformName = "Android";
        public const BuildTarget AndroidBuildTarget = BuildTarget.Android;

        public const int DefaultMaxTextureSize = 1024;
        public const int IconMaxTextureSize = 512;
        public const int BackgroundMaxTextureSize = 1024;

        public const TextureImporterFormat RgbaFormat = TextureImporterFormat.ASTC_6x6;
        public const TextureImporterFormat RgbFormat = TextureImporterFormat.ASTC_8x8;
        public const TextureImporterFormat BackgroundFormat = TextureImporterFormat.ASTC_8x8;

        public const int CompressionQuality = 50;

        public static IosTextureCompressionSettings.TextureCategory GetCategory(string assetPath)
        {
            return IosTextureCompressionSettings.GetCategory(assetPath);
        }

        public static int GetMaxTextureSize(IosTextureCompressionSettings.TextureCategory category)
        {
            switch (category)
            {
                case IosTextureCompressionSettings.TextureCategory.Icon:
                    return IconMaxTextureSize;
                case IosTextureCompressionSettings.TextureCategory.Background:
                    return BackgroundMaxTextureSize;
                default:
                    return DefaultMaxTextureSize;
            }
        }

        public static TextureImporterFormat GetFormat(
            IosTextureCompressionSettings.TextureCategory category,
            bool hasAlpha)
        {
            if (category == IosTextureCompressionSettings.TextureCategory.Background)
                return BackgroundFormat;

            return hasAlpha ? RgbaFormat : RgbFormat;
        }

        public static TextureImporterPlatformSettings CreateAndroidPlatformSettings(
            IosTextureCompressionSettings.TextureCategory category,
            bool hasAlpha,
            int? maxSizeOverride = null)
        {
            return new TextureImporterPlatformSettings
            {
                name = AndroidPlatformName,
                overridden = true,
                maxTextureSize = maxSizeOverride ?? GetMaxTextureSize(category),
                format = GetFormat(category, hasAlpha),
                textureCompression = TextureImporterCompression.Compressed,
                compressionQuality = CompressionQuality,
            };
        }

        public static AtlasSettings CreateAtlasDefaults()
        {
            return new AtlasSettings
            {
                includeInBuild = true,
                allowRotation = false,
                tightPacking = true,
                alphaDilation = true,
                padding = 4,
                readWrite = false,
                mipMaps = false,
                sRGB = true,
                filterMode = FilterMode.Bilinear,
                maxTexSize = DefaultMaxTextureSize,
                texFormat = RgbaFormat,
                compressQuality = CompressionQuality,
            };
        }

        public static void ApplyCompressTexturePanelDefaults(
            TextureImporterSettings textureSettings,
            TextureImporterPlatformSettings platformSettings)
        {
            textureSettings.textureType = TextureImporterType.Sprite;
            textureSettings.spriteMode = (int)SpriteImportMode.Single;
            textureSettings.spriteMeshType = SpriteMeshType.FullRect;
            textureSettings.alphaIsTransparency = true;
            textureSettings.readable = false;
            textureSettings.mipmapEnabled = false;
            textureSettings.filterMode = FilterMode.Bilinear;
            textureSettings.wrapMode = TextureWrapMode.Clamp;

            platformSettings.name = AndroidPlatformName;
            platformSettings.overridden = true;
            platformSettings.maxTextureSize = DefaultMaxTextureSize;
            platformSettings.format = RgbaFormat;
            platformSettings.textureCompression = TextureImporterCompression.Compressed;
            platformSettings.compressionQuality = CompressionQuality;
        }
    }
}
