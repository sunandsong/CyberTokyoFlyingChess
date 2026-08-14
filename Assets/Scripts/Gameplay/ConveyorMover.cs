using System.Collections.Generic;
using System.Linq;
using CyberTokyo.Core.Board;

namespace CyberTokyo.Gameplay
{
    /// <summary>踩到箭头格之后走传送带的路径与落点。</summary>
    public static class ConveyorMover
    {
        public static ConveyorConfigDto FindConveyorAt(BoardConfigDto board, int tileIndex)
        {
            return board.Conveyors.FirstOrDefault(c => c.TriggerTileIndex == tileIndex);
        }

        /// <summary>从箭头格铺到中心的那几格坐标，给 PieceController 播动画用</summary>
        public static IReadOnlyList<GridPos> GetPathToCenter(ConveyorConfigDto conveyor)
        {
            return BoardGeometry.ConveyorPath(conveyor.TriggerTileIndex, conveyor.Length);
        }

        /// <summary>
        /// TODO OPEN-3：棋子从传送带抵达中心之后怎么办，设计上还没定。
        /// 占位规则：下一回合从箭头格的下一格重新进环。定了真规则之后改这一个方法就行，
        /// 调用它的 GameLoopController 不用跟着改。
        /// </summary>
        public static int ResolveReentryRingIndex(ConveyorConfigDto conveyor)
        {
            return (conveyor.TriggerTileIndex + 1) % BoardGeometry.RingTileCount;
        }
    }
}
