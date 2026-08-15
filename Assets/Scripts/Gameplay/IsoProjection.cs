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
        // ponytail: 视角校准旋钮。美术（建筑/装饰楼）的等距底座实测约 1:0.6~0.65
        // （量法：底座菱形高/宽，如 tokyoTower 330px 宽、侧角到前角约 100px 半高），
        // 不是教科书 2:1。棋盘格是程序画的，角度迁就美术——对不齐就先调这个值
        public const float TileHeight = 0.62f;
        /// <summary>占位地格贴图是按 2:1（128x64）画的，投影角度改了之后要在 y 上
        /// 拉伸这个倍数才能铺满格子不留缝</summary>
        public const float TileArtStretchY = TileHeight / 0.5f;

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
