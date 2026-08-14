using System;
using CyberTokyo.Core.Board;
using UnityEngine;

namespace CyberTokyo.Gameplay
{
    /// <summary>
    /// TileColor -&gt; 显示。TileSprite 配了就用图（真美术），没配就退回纯色（占位）——
    /// 素材可以一类一类地换，不用一次到位。
    /// </summary>
    [CreateAssetMenu(fileName = "TileColorPalette", menuName = "Cyber Tokyo/Tile Color Palette")]
    public class TileColorPaletteSO : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public TileColor Color;
            public Color DisplayColor;
            /// <summary>真美术的格子贴图，按 docs/art-spec.md 的规范出图后拖进来。null = 用纯色</summary>
            public Sprite TileSprite;
        }

        public Entry[] Entries;

        public Color Resolve(TileColor color)
        {
            foreach (var entry in Entries)
            {
                if (entry.Color == color) return entry.DisplayColor;
            }
            return UnityEngine.Color.magenta; // 没配到 —— 显眼地报错，别悄悄吞掉
        }

        public Sprite ResolveSprite(TileColor color)
        {
            foreach (var entry in Entries)
            {
                if (entry.Color == color) return entry.TileSprite;
            }
            return null;
        }
    }
}
