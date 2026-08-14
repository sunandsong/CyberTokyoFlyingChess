using CyberTokyo.Core.Board;
using UnityEngine;

namespace CyberTokyo.Gameplay
{
    /// <summary>
    /// 方格坐标 -&gt; 等距（2:1 iso）屏幕坐标。对应客户端设计里的 isoProjection：
    /// 菱形格宽 1 高 0.5，(6,6) 中心落在原点，col+row 越大越靠近观察者（越靠下）。
    /// 0 号格 (12,9) 因此落在离观察者最近的外角 —— 与 geometry.ts 注释的约定一致。
    /// </summary>
    public static class IsoProjection
    {
        public const float TileWidth = 1f;
        public const float TileHeight = 0.5f;

        public static Vector3 WorldPosition(float col, float row)
        {
            float x = (col - row) * (TileWidth / 2f);
            float y = -((col + row) - (BoardGeometry.RingSide - 1)) * (TileHeight / 2f);
            return new Vector3(x, y, 0f);
        }

        public static Vector3 WorldPosition(GridPos pos) => WorldPosition(pos.Col, pos.Row);

        /// <summary>画家算法排序：col+row 越大越靠前，后画（盖在前面）</summary>
        public static int SortOrder(GridPos pos) => pos.Col + pos.Row;
    }
}
