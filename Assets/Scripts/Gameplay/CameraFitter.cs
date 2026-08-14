using UnityEngine;

namespace CyberTokyo.Gameplay
{
    /// <summary>
    /// 保证整张棋盘横向完整可见。棋盘 13 格宽（±6.5 单位），窄屏（手机竖屏）下
    /// 固定的 orthographicSize 会把左右臂裁出屏幕外 —— 按当前宽高比反推需要的
    /// 纵向半高。编辑器宽视图和手机窄视图用同一套逻辑，不再各看各的。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFitter : MonoBehaviour
    {
        /// <summary>需要装下的横向半宽：13/2 + 一点余量</summary>
        [SerializeField] private float halfBoardWidth = 6.7f;
        /// <summary>宽屏下的纵向半高下限（棋盘 13 格高 + 上下 UI 呼吸空间）</summary>
        [SerializeField] private float minHalfHeight = 8f;

        private Camera _camera;
        private float _lastAspect;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            Fit();
        }

        private void Update()
        {
            // 转屏/改窗口时宽高比会变，变了才重算
            if (!Mathf.Approximately(_camera.aspect, _lastAspect)) Fit();
        }

        private void Fit()
        {
            _lastAspect = _camera.aspect;
            _camera.orthographicSize = Mathf.Max(minHalfHeight, halfBoardWidth / _camera.aspect);
        }
    }
}
