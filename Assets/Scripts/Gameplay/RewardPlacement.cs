using System.Collections.Generic;
using System.Linq;
using CyberTokyo.Core.Board;
using CyberTokyo.Core.Reward;
using UnityEngine;

namespace CyberTokyo.Gameplay
{
    /// <summary>
    /// TEMP for OPEN-1（哪几格带奖励未定）：默认配置里所有 tile 的 OnPass/OnLand
    /// 都是空的，真跑起来一个奖励都摸不到。这里在本局开局时随机挑
    /// density.Evaluate(level) 个 normal 格塞 OnLand 奖励，纯粹为了让 Phase 3
    /// 的闭环能玩。后台定出真正的摆放算法、下发的 tile 自带 OnPass/OnLand 之后，
    /// 删掉这个类，GameLoopController 里调用它的那一行也删掉。
    /// </summary>
    public static class RewardPlacement
    {
        public static void ApplyTemporaryPlacement(BoardConfigDto board, RewardConfigDto reward, int level)
        {
            var normalTiles = board.Tiles.Where(t => t.Kind == TileKind.Normal).ToList();
            int count = Mathf.Clamp(Mathf.RoundToInt(reward.Density.Evaluate(level)), 0, normalTiles.Count);

            for (int i = 0; i < normalTiles.Count; i++)
            {
                int j = Random.Range(i, normalTiles.Count);
                (normalTiles[i], normalTiles[j]) = (normalTiles[j], normalTiles[i]);
            }

            var eligible = reward.Pool.Where(p => p.UnlockLevel <= level).ToList();
            if (eligible.Count == 0) return;
            float totalWeight = eligible.Sum(p => p.Weight);

            int amount = Mathf.Max(1, Mathf.CeilToInt(reward.ValueScale.Evaluate(level)));

            for (int i = 0; i < count; i++)
            {
                normalTiles[i].OnLand = new RewardAmountDto
                {
                    Kind = PickWeighted(eligible, totalWeight),
                    Amount = amount,
                };
            }
        }

        private static RewardKind PickWeighted(List<RewardPoolEntryDto> eligible, float totalWeight)
        {
            float r = Random.value * totalWeight;
            float cumulative = 0f;
            foreach (var entry in eligible)
            {
                cumulative += entry.Weight;
                if (r <= cumulative) return entry.Kind;
            }
            return eligible[eligible.Count - 1].Kind;
        }
    }
}
