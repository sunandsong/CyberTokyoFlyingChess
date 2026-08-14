using CyberTokyo.Core.Board;
using CyberTokyo.Core.State;

namespace CyberTokyo.Core.Reward
{
    /// <summary>
    /// 奖励要去哪儿的抽象。Phase 3 只有 RewardLedger 这一个实现（纯内存计数），
    /// 以后要接持久化/后端上报，加一个新实现就行，不用动发奖那边的调用代码。
    /// </summary>
    public interface IRewardSink
    {
        void Grant(PlayerState player, RewardAmountDto reward);
    }
}
