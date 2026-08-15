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
        /// <summary>可见横向半宽。刻意小于棋盘半宽（6.5）—— 镜头拉近跟着棋子走
        /// （见 CameraFollow），不再追求一屏装下整张棋盘</summary>
        [SerializeField] private float halfBoardWidth = 2.6f;
        /// <summary>宽屏（编辑器）下的纵向半高下限</summary>
        [SerializeField] private float minHalfHeight = 4.2f;
        /// <summary>窄屏（手机竖屏）下的纵向半高上限（安全网，正常不触发——一旦
        /// 触发就意味着横向会被顶到比 halfBoardWidth 还窄，建筑被裁边）</summary>
        [SerializeField] private float maxHalfHeight = 6f;

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
            _camera.orthographicSize = Mathf.Clamp(halfBoardWidth / _camera.aspect, minHalfHeight, maxHalfHeight);
        }
    }
}
