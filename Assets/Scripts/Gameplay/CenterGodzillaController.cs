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
        /// <summary>atomicBreath 状态的喷吐粒子，builder 在 prefab 里配好绑进来</summary>
        [SerializeField] private ParticleSystem breathParticles;

        private List<CenterStateDto> _states;
        private int _currentIndex;
        private CenterStateVisualSO _visuals;
        private Sprite _placeholderSprite;
        private Color _stateBaseColor = Color.white;
        private Vector3 _groundPosition;
        private Vector3 _placeholderScale;

        public string CurrentStateKey => _states[_currentIndex].Key;

        public void Initialize(CenterConfigDto config, CenterStateVisualSO visuals)
        {
            _states = config.States;
            _currentIndex = 0;
            _visuals = visuals;
            if (spriteRenderer != null) _placeholderSprite = spriteRenderer.sprite;
            // 记住脚下位置和占位块的拉伸比例：真图（底边中点锚点、1:1 比例）和
            // 占位块（居中锚点、1.5x2 拉伸、上抬半身）的摆法不同，按当前用的是哪种切换
            _groundPosition = transform.position;
            _placeholderScale = transform.localScale;
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
                _stateBaseColor = Color.white;
                transform.localScale = Vector3.one;
                transform.position = _groundPosition;
            }
            else
            {
                spriteRenderer.sprite = _placeholderSprite;
                _stateBaseColor = PlaceholderColorFor(CurrentStateKey);
                transform.localScale = _placeholderScale;
                transform.position = _groundPosition + new Vector3(0f, 0.8f, 0f);
            }
            spriteRenderer.color = _stateBaseColor;

            if (breathParticles != null)
            {
                if (CurrentStateKey == "atomicBreath") breathParticles.Play();
                else breathParticles.Stop();
            }
        }

        /// <summary>激动状态（angry/atomicBreath）时亮度脉冲，配合 Bloom 泛光</summary>
        private void Update()
        {
            if (spriteRenderer == null || _states == null) return;
            bool agitated = CurrentStateKey == "angry" || CurrentStateKey == "atomicBreath";
            if (!agitated)
            {
                spriteRenderer.color = _stateBaseColor;
                return;
            }

            float k = 1f + 0.25f * Mathf.Sin(Time.time * 5f);
            spriteRenderer.color = new Color(
                Mathf.Min(1f, _stateBaseColor.r * k),
                Mathf.Min(1f, _stateBaseColor.g * k),
                Mathf.Min(1f, _stateBaseColor.b * k),
                _stateBaseColor.a);
        }

        private static Color PlaceholderColorFor(string key)
        {
            switch (key)
            {
                case "sleeping": return new Color(0.32f, 0.32f, 0.48f);
                case "angry": return new Color(1.00f, 0.28f, 0.35f);
                case "atomicBreath": return new Color(1.00f, 0.95f, 0.35f);
                case "pleased": return new Color(0.25f, 0.95f, 0.60f);
                default: return Color.gray;
            }
        }
    }
}
