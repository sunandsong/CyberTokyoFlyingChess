using UnityEditor;
using UnityEngine;

namespace CyberTokyo.Editor
{
    /// <summary>
    /// Assets/Art/Sprites/ 下的贴图导入规范，自动套用 —— 你（或 AI）出的图拖进对应
    /// 文件夹就是对的设置，不需要手动改 Inspector。规则与 docs/art-spec.md 一一对应。
    ///
    /// 用 AssetPostprocessor 而不是 Preset：Preset 要靠人记得去套，这个是拖进来就生效，
    /// 而且规则进版本库，换机器/换人不丢。
    /// </summary>
    public class ArtImportPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Art/Sprites/")) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Compressed;

            importer.spritePixelsPerUnit = PixelsPerUnitFor(assetPath);

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)AlignmentFor(assetPath);
            importer.SetTextureSettings(settings);
        }

        /// <summary>
        /// Reward 图标是 UI 尺度（64px/100ppu）；Center 的哥斯拉素材实图约 500px 宽，
        /// 按 256ppu 折成 ~2 世界单位，正好占住十字中心区；其余 128 —— 1 格 = 1 世界单位。
        /// </summary>
        private static float PixelsPerUnitFor(string path)
        {
            if (path.Contains("/Reward/")) return 100f;
            if (path.Contains("/Center/")) return 256f;
            return 128f;
        }

        /// <summary>
        /// 立着的东西（棋子/建筑/中心哥斯拉）锚点在底边中点 —— 站在格子上，换图不跳位；
        /// 平铺的东西（地格/图标/UI）锚点居中。
        /// </summary>
        private static SpriteAlignment AlignmentFor(string path)
        {
            if (path.Contains("/Pieces/") || path.Contains("/Buildings/") || path.Contains("/Center/"))
            {
                return SpriteAlignment.BottomCenter;
            }
            return SpriteAlignment.Center;
        }
    }
}
