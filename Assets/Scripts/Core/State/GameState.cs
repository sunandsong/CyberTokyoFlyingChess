using System.Collections.Generic;
using CyberTokyo.Core.Board;

namespace CyberTokyo.Core.State
{
    /// <summary>一整局的运行时状态：玩家、等级、当前生效的配置。</summary>
    public class GameState
    {
        /// <summary>固定顺序 green/yellow/red/blue，跟 TILE_COLORS 对应</summary>
        public static readonly TileColor[] TurnOrder =
        {
            TileColor.Green,
            TileColor.Yellow,
            TileColor.Red,
            TileColor.Blue,
        };

        public readonly List<PlayerState> Players;

        /// <summary>奖励曲线用的等级，1 起。OPEN-6 未定胜利条件前，等级只升不重置</summary>
        public int Level = 1;

        public BoardConfigDto Board;
        public Reward.RewardConfigDto Reward;

        public GameState(BoardConfigDto board, Reward.RewardConfigDto reward)
        {
            Board = board;
            Reward = reward;
            Players = new List<PlayerState>(TurnOrder.Length);
            foreach (var color in TurnOrder)
            {
                Players.Add(new PlayerState(color));
            }
        }
    }
}
