using UnityEngine;

namespace CyberTokyo.Gameplay
{
    /// <summary>
    /// 镜头平滑跟随当前回合的棋子（Monopoly GO 式运镜）。竖屏手机装不下整张宽扁的
    /// 等距棋盘，与其拉远看小格子，不如拉近跟着走。夹紧范围防止镜头跑出棋盘太远。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private float smoothTime = 0.35f;
        [SerializeField] private Vector2 clampX = new Vector2(-3.4f, 3.4f);
        [SerializeField] private Vector2 clampY = new Vector2(-2.2f, 2.6f);

        private Transform _target;
        private Vector3 _velocity;

        public void SetTarget(Transform target) => _target = target;

        private void LateUpdate()
        {
            if (_target == null) return;
            var goal = new Vector3(
                Mathf.Clamp(_target.position.x, clampX.x, clampX.y),
                Mathf.Clamp(_target.position.y + 0.4f, clampY.x, clampY.y),
                transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, goal, ref _velocity, smoothTime);
        }
    }
}
