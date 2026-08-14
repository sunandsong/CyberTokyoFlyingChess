using System;
using System.Collections.Generic;
using CyberTokyo.Core.Board;

namespace CyberTokyo.Core.Reward
{
    /// <summary>
    /// 奖励配置的形状 —— 移植自 fly-game-admin/src/types.ts 的 RewardConfig 一族。
    /// </summary>

    /// <summary>线性曲线 base + perLevel * (level - 1)。只做线性是刻意的，见后台注释</summary>
    [Serializable]
    public class LinearCurveDto
    {
        public float Base;
        public float PerLevel;

        public float Evaluate(int level) => Base + PerLevel * (level - 1);
    }

    /// <summary>一条奖励池项：某个类型、相对权重、从哪一级解锁</summary>
    [Serializable]
    public class RewardPoolEntryDto
    {
        public RewardKind Kind;
        /// <summary>相对权重，恒为正</summary>
        public float Weight;
        /// <summary>从这一级起进池子。1 = 开局就有</summary>
        public int UnlockLevel;
    }

    [Serializable]
    public class RewardConfigDto
    {
        /// <summary>数值倍率随等级的曲线。恒为正、随等级单调不减</summary>
        public LinearCurveDto ValueScale = new LinearCurveDto();
        /// <summary>48 格里有多少格带奖励。夹在 [0, 48] 内</summary>
        public LinearCurveDto Density = new LinearCurveDto();
        public List<RewardPoolEntryDto> Pool = new List<RewardPoolEntryDto>();
        /// <summary>传送带终点格上放什么。硬编码布局，不走随机池</summary>
        public RewardAmountDto ConveyorEndReward;
    }
}
