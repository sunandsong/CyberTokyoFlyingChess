using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CyberTokyo.Gameplay
{
    /// <summary>一枚棋子的表现（有真图用真图，没有就白圈+色心占位）与逐格挪动动画。</summary>
    public class PieceController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        public void SetColor(Color color)
        {
            if (spriteRenderer != null) spriteRenderer.color = color;
        }

        /// <summary>
        /// 换成真美术：根渲染器直接用图、恢复 1:1 比例（占位圆的 0.6 缩放是给
        /// 128px 白圆配的，真图按 art-spec 96px 出图自带正确尺寸），色心子节点藏掉。
        /// </summary>
        public void SetSprite(Sprite sprite)
        {
            if (sprite == null) return;

            var root = GetComponent<SpriteRenderer>();
            if (root != null)
            {
                root.sprite = sprite;
                root.color = Color.white;
                transform.localScale = Vector3.one;
            }
            if (spriteRenderer != null && spriteRenderer != root) spriteRenderer.enabled = false;
        }

        public void SnapTo(Vector3 worldPosition)
        {
            transform.position = worldPosition;
        }

        /// <summary>依次挪到每个 waypoint，每挪到一个就回调一次（用来在中途格触发 OnPass）。</summary>
        public IEnumerator StepAlong(IReadOnlyList<Vector3> waypoints, float stepDuration, Action<int> onStepLanded)
        {
            for (int i = 0; i < waypoints.Count; i++)
            {
                Vector3 start = transform.position;
                Vector3 end = waypoints[i];
                float t = 0f;
                while (t < 1f)
                {
                    t += Time.deltaTime / stepDuration;
                    transform.position = Vector3.Lerp(start, end, Mathf.Clamp01(t));
                    yield return null;
                }
                transform.position = end;
                onStepLanded?.Invoke(i);
            }
        }
    }
}
