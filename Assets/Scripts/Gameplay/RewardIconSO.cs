using System;
using CyberTokyo.Core.Board;
using UnityEngine;

namespace CyberTokyo.Gameplay
{
    /// <summary>
    /// RewardKind -&gt; 奖励图标。用在带奖励的格子上（以后也给 HUD 用）。
    /// 配了用图、没配退白色小圆点占位。
    /// </summary>
    [CreateAssetMenu(fileName = "RewardIcons", menuName = "Cyber Tokyo/Reward Icons")]
    public class RewardIconSO : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public RewardKind Kind;
            public Sprite Sprite;
        }

        public Entry[] Entries;

        public Sprite Resolve(RewardKind kind)
        {
            foreach (var entry in Entries)
            {
                if (entry.Kind == kind) return entry.Sprite;
            }
            return null;
        }
    }
}
