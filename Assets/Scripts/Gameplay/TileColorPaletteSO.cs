using System;
using CyberTokyo.Core.Board;
using UnityEngine;

namespace CyberTokyo.Gameplay
{
    /// <summary>
    /// 占位美术：TileColor -&gt; 纯色。Phase 5 真美术接进来后，这里会加一个
    /// TileColor -&gt; Sprite 的查找表，TileView 从「设置颜色」改成「设置贴图」，
    /// 但 BoardRenderer 调用它的方式不用变。
    /// </summary>
    [CreateAssetMenu(fileName = "TileColorPalette", menuName = "Cyber Tokyo/Tile Color Palette")]
    public class TileColorPaletteSO : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public TileColor Color;
            public Color DisplayColor;
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
    }
}
