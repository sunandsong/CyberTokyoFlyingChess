using System;
using System.Collections.Generic;

namespace CyberTokyo.Core.Board
{
    /// <summary>
    /// Immutable grid coordinate. Deliberately not UnityEngine.Vector2Int —
    /// this whole class has zero UnityEngine dependency so it can be covered
    /// by plain EditMode/headless tests, mirroring fly-game-admin's
    /// src/geometry.ts, which this is a straight port of.
    ///
    /// Port source: fly-game-admin/src/geometry.ts. Keep in sync — see
    /// BoardGeometryParityTests for the reconciliation check against a live
    /// /api/game/config snapshot.
    /// </summary>
    public readonly struct GridPos : IEquatable<GridPos>
    {
        public readonly int Col;
        public readonly int Row;

        public GridPos(int col, int row)
        {
            Col = col;
            Row = row;
        }

        public bool Equals(GridPos other) => Col == other.Col && Row == other.Row;
        public override bool Equals(object obj) => obj is GridPos other && Equals(other);
        public override int GetHashCode() => (Col, Row).GetHashCode();
        public override string ToString() => $"({Col}, {Row})";
    }

    public enum CornerSlot
    {
        TopLeft,
        TopRight,
        BottomRight,
        BottomLeft,
    }

    public readonly struct CornerArea
    {
        public readonly CornerSlot Slot;
        public readonly int Col;
        public readonly int Row;
        public readonly int Size;

        public CornerArea(CornerSlot slot, int col, int row, int size)
        {
            Slot = slot;
            Col = col;
            Row = row;
            Size = size;
        }
    }

    /// <summary>
    /// 十字棋盘的几何 —— 移植自 fly-game-admin/src/geometry.ts。改动前先去看后台那份，
    /// 两边必须保持一致（后台画的棋盘和客户端跑出来的必须逐格相同）。
    ///
    /// 棋盘是十字形不是方框：十字嵌在 13x13 里，四角各缺 3x3，臂宽 7 格，
    /// 周长恰好 48（方框周长同样是 48，光数格子看不出形状错）。
    /// </summary>
    public static class BoardGeometry
    {
        public const int RingTileCount = 48;
        public const int RingSide = 13;
        public const int ArmMin = 3;
        public const int ArmMax = RingSide - 1 - ArmMin; // 9

        /// <summary>每条通往中心的路铺几格。逻辑距离是这个数 + 1（中心自己算一格）</summary>
        public const int ConveyorLength = 4;

        /// <summary>棋盘正中，哥斯拉所在处</summary>
        public static readonly GridPos BoardCenter = new GridPos((RingSide - 1) / 2, (RingSide - 1) / 2);

        public static readonly IReadOnlyList<CornerArea> CornerAreas = BuildCornerAreas();

        /// <summary>
        /// 十字轮廓的 12 个拐点，按行进顺序首尾相接。
        ///
        /// 0 号格是 スタート 牌坊，落在等距投影下离观察者最近的那个外角 (12, 9)。
        /// 从那里先沿右边向上 —— 这个方向使得四角建筑落在 0/12/24/36、
        /// 箭头格落在 3/15/27/39、内拐角落在 9/21/33/45。
        ///
        /// 声明顺序有讲究：C# 静态字段按源码里出现的先后顺序初始化，不是按依赖关系。
        /// 这个数组必须排在下面 RingPath 前面 —— RingPath 的初始化要调用 BuildRingPath()，
        /// 而那个方法读的就是这个数组，反过来放就会在它还是 null 时被读，直接空引用崩掉
        /// （踩过一次的坑，别再挪回去）。
        /// </summary>
        private static readonly GridPos[] OutlineCorners =
        {
            new GridPos(RingSide - 1, ArmMax), // スタート
            new GridPos(RingSide - 1, ArmMin),
            new GridPos(ArmMax, ArmMin), // 内拐角
            new GridPos(ArmMax, 0),
            new GridPos(ArmMin, 0),
            new GridPos(ArmMin, ArmMin), // 内拐角
            new GridPos(0, ArmMin),
            new GridPos(0, ArmMax),
            new GridPos(ArmMin, ArmMax), // 内拐角
            new GridPos(ArmMin, RingSide - 1),
            new GridPos(ArmMax, RingSide - 1),
            new GridPos(ArmMax, ArmMax), // 内拐角
        };

        private static readonly IReadOnlyList<GridPos> RingPath = BuildRingPath();

        private static int Sign(int v) => Math.Sign(v);

        private static IReadOnlyList<GridPos> BuildRingPath()
        {
            var path = new List<GridPos>();

            for (int i = 0; i < OutlineCorners.Length; i++)
            {
                GridPos from = OutlineCorners[i];
                GridPos to = OutlineCorners[(i + 1) % OutlineCorners.Length];

                int stepCol = Sign(to.Col - from.Col);
                int stepRow = Sign(to.Row - from.Row);
                int steps = Math.Abs(to.Col - from.Col) + Math.Abs(to.Row - from.Row);

                // 含起点不含终点，终点归下一段，拐点才不会数两次
                for (int s = 0; s < steps; s++)
                {
                    path.Add(new GridPos(from.Col + stepCol * s, from.Row + stepRow * s));
                }
            }

            if (path.Count != RingTileCount)
            {
                throw new InvalidOperationException(
                    $"十字轮廓算出 {path.Count} 格，与 RingTileCount={RingTileCount} 不符");
            }

            return path;
        }

        /// <summary>第 index 格在方格上的坐标。超出 [0,48) 自动绕圈</summary>
        public static GridPos RingPosition(int index)
        {
            int i = ((index % RingTileCount) + RingTileCount) % RingTileCount;
            return RingPath[i];
        }

        /// <summary>反查：这个坐标是环路第几格？不在环路上返回 -1</summary>
        public static int RingIndexAt(GridPos pos)
        {
            for (int i = 0; i < RingPath.Count; i++)
            {
                if (RingPath[i].Equals(pos)) return i;
            }
            return -1;
        }

        /// <summary>从箭头格朝中心铺的那几格坐标，从下一格起</summary>
        public static IReadOnlyList<GridPos> ConveyorPath(int triggerTileIndex, int length)
        {
            GridPos start = RingPosition(triggerTileIndex);
            int stepCol = Sign(BoardCenter.Col - start.Col);
            int stepRow = Sign(BoardCenter.Row - start.Row);

            var path = new List<GridPos>(length);
            for (int step = 1; step <= length; step++)
            {
                path.Add(new GridPos(start.Col + stepCol * step, start.Row + stepRow * step));
            }
            return path;
        }

        /*
         * 四个角落 —— 十字形缺掉的那四块 3x3。
         * 建筑画在这里，不在环路格上。
         */
        private static IReadOnlyList<CornerArea> BuildCornerAreas()
        {
            const int low = 0;
            int high = ArmMax + 1;

            return new[]
            {
                new CornerArea(CornerSlot.TopLeft, low, low, ArmMin),
                new CornerArea(CornerSlot.TopRight, high, low, ArmMin),
                new CornerArea(CornerSlot.BottomRight, high, high, ArmMin),
                new CornerArea(CornerSlot.BottomLeft, low, high, ArmMin),
            };
        }

        /// <summary>离某个环路格最近的角落</summary>
        public static CornerSlot CornerSlotNearTile(int index)
        {
            GridPos pos = RingPosition(index);

            CornerArea best = CornerAreas[0];
            double bestDistance = double.PositiveInfinity;

            foreach (var area in CornerAreas)
            {
                double centerCol = area.Col + (area.Size - 1) / 2.0;
                double centerRow = area.Row + (area.Size - 1) / 2.0;
                double distance = Math.Abs(pos.Col - centerCol) + Math.Abs(pos.Row - centerRow);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = area;
                }
            }
            return best.Slot;
        }

        /*
         * 位置分类 —— 校验特殊格摆得对不对要用。刻意按几何判断而不是硬编码序号，
         * 这样允许整体旋转编号，但拦得住把箭头格摆到拐角这类会让传送带歪掉的配置。
         */

        /// <summary>内拐角（十字的凹角），四个。自由传送格/交界格该待的地方</summary>
        public static bool IsConcaveCorner(GridPos pos)
        {
            bool OnArmEdge(int v) => v == ArmMin || v == ArmMax;
            return OnArmEdge(pos.Col) && OnArmEdge(pos.Row);
        }

        /// <summary>外凸角，八个。四角建筑该待的地方（另外四个是臂端的转角）</summary>
        public static bool IsConvexCorner(GridPos pos)
        {
            bool Outer(int v) => v == 0 || v == RingSide - 1;
            bool ArmEdge(int v) => v == ArmMin || v == ArmMax;
            return (Outer(pos.Col) && ArmEdge(pos.Row)) || (Outer(pos.Row) && ArmEdge(pos.Col));
        }

        /// <summary>
        /// 臂的正中 —— 与中心在某个轴上对齐。箭头格必须在这里，否则从它铺出去的
        /// 传送带不是笔直指向中心的。
        /// </summary>
        public static bool IsArmMidpoint(GridPos pos)
        {
            return pos.Col == BoardCenter.Col || pos.Row == BoardCenter.Row;
        }
    }
}
