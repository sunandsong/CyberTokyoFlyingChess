using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CyberTokyo.Gameplay
{
    /// <summary>一枚棋子的占位表现（纯色圆点）与逐格挪动动画。</summary>
    public class PieceController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        public void SetColor(Color color)
        {
            if (spriteRenderer != null) spriteRenderer.color = color;
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
