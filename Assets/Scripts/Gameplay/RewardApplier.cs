using CyberTokyo.Core.Board;
using CyberTokyo.Core.Reward;
using CyberTokyo.Core.State;

namespace CyberTokyo.Gameplay
{
    /// <summary>把"这格有奖励"变成"玩家账上多了点东西"。落地逻辑集中在这一个类，
    /// GameLoopController 不用知道 OnPass/OnLand/传送带终点奖励内部怎么记账。</summary>
    public class RewardApplier
    {
        private readonly IRewardSink _sink;

        public RewardApplier(IRewardSink sink)
        {
            _sink = sink;
        }

        public void ApplyOnPass(PlayerState player, TileConfigDto tile)
        {
            if (tile.OnPass != null) _sink.Grant(player, tile.OnPass);
        }

        public void ApplyOnLand(PlayerState player, TileConfigDto tile)
        {
            if (tile.OnLand != null) _sink.Grant(player, tile.OnLand);
        }

        public void ApplyConveyorEnd(PlayerState player, RewardAmountDto conveyorEndReward)
        {
            _sink.Grant(player, conveyorEndReward);
        }
    }
}
