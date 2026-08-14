using System;
using CyberTokyo.Core.Board;
using UnityEngine;

namespace CyberTokyo.Gameplay
{
    /// <summary>
    /// TileColor -&gt; 棋子贴图（可爱动物飞行器，见设计文档）。
    /// 配了用图、没配退白圈+色心的占位圆点。
    /// </summary>
    [CreateAssetMenu(fileName = "PieceVisuals", menuName = "Cyber Tokyo/Piece Visuals")]
    public class PieceVisualSO : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public TileColor Color;
            public Sprite Sprite;
        }

        public Entry[] Entries;

        public Sprite Resolve(TileColor color)
        {
            foreach (var entry in Entries)
            {
                if (entry.Color == color) return entry.Sprite;
            }
            return null;
        }
    }
}
