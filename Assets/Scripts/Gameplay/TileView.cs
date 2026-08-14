using CyberTokyo.Core.Board;
using UnityEngine;

namespace CyberTokyo.Gameplay
{
    /// <summary>一格棋盘的占位表现：纯色 SpriteRenderer。交界格（双色）先只取第一个颜色——
    /// 形式 1 默认配置里不存在交界格，真用上时再决定怎么画对角双色。</summary>
    public class TileView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        public TileConfigDto Tile { get; private set; }

        public void Initialize(TileConfigDto tile, TileColorPaletteSO palette)
        {
            Tile = tile;
            if (spriteRenderer != null && tile.Colors.Count > 0)
            {
                spriteRenderer.color = palette.Resolve(tile.Colors[0]);
            }
        }
    }
}
