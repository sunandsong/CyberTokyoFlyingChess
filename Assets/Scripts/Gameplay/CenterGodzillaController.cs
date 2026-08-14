using System.Collections.Generic;
using CyberTokyo.Core.Board;
using UnityEngine;

namespace CyberTokyo.Gameplay
{
    /// <summary>
    /// 中心哥斯拉的状态占位机。真正的素材（每个状态一张图）走 Phase 5/7 的
    /// ICenterStateVisualProvider，这里先用纯色区分状态，让状态切换本身可见、可测。
    /// </summary>
    public class CenterGodzillaController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        private List<CenterStateDto> _states;
        private int _currentIndex;
        private CenterStateVisualSO _visuals;
        private Sprite _placeholderSprite;

        public string CurrentStateKey => _states[_currentIndex].Key;

        public void Initialize(CenterConfigDto config, CenterStateVisualSO visuals)
        {
            _states = config.States;
            _currentIndex = 0;
            _visuals = visuals;
            if (spriteRenderer != null) _placeholderSprite = spriteRenderer.sprite;
            ApplyVisual();
        }

        /// <summary>
        /// TODO OPEN-2：真正的切换触发条件未定（设计文档只给了状态序列，没给触发规则）。
        /// 占位规则：任何棋子从传送带抵达中心，状态往前推一格，到底再绕回第一个。
        /// 定了真规则之后改这一个方法，GameLoopController 调用它的地方不用跟着改。
        /// </summary>
        public void OnPieceReachedCenter()
        {
            if (_states == null || _states.Count == 0) return;
            _currentIndex = (_currentIndex + 1) % _states.Count;
            ApplyVisual();
        }

        private void ApplyVisual()
        {
            if (spriteRenderer == null) return;

            var sprite = _visuals != null ? _visuals.Resolve(CurrentStateKey) : null;
            if (sprite != null)
            {
                spriteRenderer.sprite = sprite;
                spriteRenderer.color = Color.white;
            }
            else
            {
                spriteRenderer.sprite = _placeholderSprite;
                spriteRenderer.color = PlaceholderColorFor(CurrentStateKey);
            }
        }

        private static Color PlaceholderColorFor(string key)
        {
            switch (key)
            {
                case "sleeping": return new Color(0.30f, 0.30f, 0.42f);
                case "angry": return new Color(0.80f, 0.20f, 0.20f);
                case "atomicBreath": return new Color(0.95f, 0.90f, 0.20f);
                case "pleased": return new Color(0.30f, 0.80f, 0.45f);
                default: return Color.gray;
            }
        }
    }
}
