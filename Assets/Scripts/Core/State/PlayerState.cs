using CyberTokyo.Core.Board;

namespace CyberTokyo.Core.State
{
    /// <summary>
    /// 一个玩家的运行时状态。纯 C#，不落盘 —— 每局重开就是新的 GameState/PlayerState，
    /// 不要跟 Phase 2 的 *Dto（配置数据）混在一起。
    /// </summary>
    public class PlayerState
    {
        public readonly TileColor Color;

        /// <summary>当前在 48 格环路上的序号。开局站在 0 号格（スタート）</summary>
        public int RingIndex;

        /// <summary>本局累计到的奖励，Key 是 RewardKind 的 int 值，避免又建一个 Dictionary&lt;RewardKind,...&gt; 装箱枚举的坑</summary>
        public readonly System.Collections.Generic.Dictionary<RewardKind, int> RewardTotals =
            new System.Collections.Generic.Dictionary<RewardKind, int>();

        public PlayerState(TileColor color)
        {
            Color = color;
            RingIndex = 0;
        }

        public void AddReward(RewardKind kind, int amount)
        {
            RewardTotals.TryGetValue(kind, out int current);
            RewardTotals[kind] = current + amount;
        }
    }
}
