using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    /// <summary>
    /// iOS 贴图/图集默认压缩配置（ASTC + 分级 MaxSize）。
    /// </summary>
    public static class IosTextureCompressionSettings
    {
        public const string IosPlatformName = "iPhone";
        public const BuildTarget IosBuildTarget = BuildTarget.iOS;

        public const int DefaultMaxTextureSize = 1024;
        public const int IconMaxTextureSize = 512;
        public const int BackgroundMaxTextureSize = 1024;

        public const TextureImporterFormat RgbaFormat = TextureImporterFormat.ASTC_6x6;
        public const TextureImporterFormat RgbFormat = TextureImporterFormat.ASTC_8x8;
        public const TextureImporterFormat BackgroundFormat = TextureImporterFormat.ASTC_8x8;

        public const int CompressionQuality = 50;

        public enum TextureCategory
        {
            Icon,
            Background,
            Spine,
            Default
        }

        public static TextureCategory GetCategory(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return TextureCategory.Default;

            string path = assetPath.Replace('\\', '/').ToLowerInvariant();

            if (path.Contains("/bankicon/") || path.Contains("/icon/"))
                return TextureCategory.Icon;

            if (path.Contains("/cutscene/")
                || path.Contains("/spine/")
                || path.EndsWith("/bg.png")
                || path.Contains("guidebg")
                || path.Contains("zhuanchang"))
                return TextureCategory.Background;

            if (path.Contains("/prefabs/spine/"))
                return TextureCategory.Spine;

            return TextureCategory.Default;
        }

        public static int GetMaxTextureSize(TextureCategory category)
        {
            switch (category)
            {
                case TextureCategory.Icon:
                    return IconMaxTextureSize;
                case TextureCategory.Background:
                    return BackgroundMaxTextureSize;
                default:
                    return DefaultMaxTextureSize;
            }
        }

        public static TextureImporterFormat GetFormat(TextureCategory category, bool hasAlpha)
        {
            if (category == TextureCategory.Background)
                return BackgroundFormat;

            return hasAlpha ? RgbaFormat : RgbFormat;
        }

        public static TextureImporterPlatformSettings CreateIosPlatformSettings(
            TextureCategory category,
            bool hasAlpha,
            int? maxSizeOverride = null)
        {
            return new TextureImporterPlatformSettings
            {
                name = IosPlatformName,
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

            platformSettings.name = IosPlatformName;
            platformSettings.overridden = true;
            platformSettings.maxTextureSize = DefaultMaxTextureSize;
            platformSettings.format = RgbaFormat;
            platformSettings.textureCompression = TextureImporterCompression.Compressed;
            platformSettings.compressionQuality = CompressionQuality;
        }
    }
}
