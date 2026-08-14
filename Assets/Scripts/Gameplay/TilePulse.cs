using UnityEngine;

namespace CyberTokyo.Gameplay
{
    /// <summary>
    /// 亮度呼吸脉冲。挂在箭头格上提示"踩我有事发生"，配合 Bloom 后处理，
    /// 亮到峰值时会泛出辉光。基色取挂上时刻的当前色，所以真美术换上后照样能用。
    /// </summary>
    public class TilePulse : MonoBehaviour
    {
        [SerializeField] private float speed = 3f;
        [SerializeField] private float amplitude = 0.22f;

        private SpriteRenderer _renderer;
        private Color _baseColor;
        private float _phase;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            if (_renderer != null) _baseColor = _renderer.color;
            _phase = Random.value * Mathf.PI * 2f; // 各格错开相位，别一起闪
        }

        private void Update()
        {
            if (_renderer == null) return;
            float k = 1f + amplitude * Mathf.Sin(Time.time * speed + _phase);
            _renderer.color = new Color(
                Mathf.Min(1f, _baseColor.r * k),
                Mathf.Min(1f, _baseColor.g * k),
                Mathf.Min(1f, _baseColor.b * k),
                _baseColor.a);
        }
    }
}
