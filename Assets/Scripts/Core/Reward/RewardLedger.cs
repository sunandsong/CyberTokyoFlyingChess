using CyberTokyo.Core.Board;
using CyberTokyo.Core.State;

namespace CyberTokyo.Core.Reward
{
    /// <summary>本局内存计数，不落盘。一局重开就清空，这是当前唯一的 IRewardSink 实现。</summary>
    public class RewardLedger : IRewardSink
    {
        public void Grant(PlayerState player, RewardAmountDto reward)
        {
            if (reward == null) return;
            player.AddReward(reward.Kind, reward.Amount);
        }
    }
}
