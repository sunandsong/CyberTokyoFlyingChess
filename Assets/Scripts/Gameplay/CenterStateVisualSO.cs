using System;
using UnityEngine;

namespace CyberTokyo.Gameplay
{
    /// <summary>
    /// 中心状态 key（sleeping/angry/atomicBreath/pleased，可自定义）-&gt; 哥斯拉贴图。
    /// 四张图必须同尺寸画布、同锚点位置，切换状态时才不会跳位（见 docs/art-spec.md）。
    /// 配了用图、没配退纯色占位。这是本地版查找表；Phase 7 会加走后台
    /// /api/game/asset 的远程版。
    /// </summary>
    [CreateAssetMenu(fileName = "CenterStateVisuals", menuName = "Cyber Tokyo/Center State Visuals")]
    public class CenterStateVisualSO : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string StateKey;
            public Sprite Sprite;
        }

        public Entry[] Entries;

        public Sprite Resolve(string stateKey)
        {
            foreach (var entry in Entries)
            {
                if (entry.StateKey == stateKey) return entry.Sprite;
            }
            return null;
        }
    }
}
