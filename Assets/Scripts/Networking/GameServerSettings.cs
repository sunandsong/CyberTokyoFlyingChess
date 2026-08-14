using UnityEngine;

namespace CyberTokyo.Networking
{
    /// <summary>
    /// 后台地址配置。放 Assets/Resources/Data/ 下、运行时 Resources.Load
    /// （原因见 [[unity-scene-ref-serialization-gotcha]]：场景绑不住这类 SO 引用）。
    ///
    /// 本地开发指向 wrangler dev；部署后换成 *.workers.dev 的 https 地址 ——
    /// 真机测试必须用后者，手机访问不到电脑的 localhost，而且 iOS ATS/Android
    /// 明文限制对裸 http 有额外要求。
    /// </summary>
    [CreateAssetMenu(fileName = "GameServerSettings", menuName = "Cyber Tokyo/Game Server Settings")]
    public class GameServerSettings : ScriptableObject
    {
        [Tooltip("后台根地址，不带末尾斜杠。留空 = 不走网络，直接用本地兜底配置")]
        public string BaseUrl = "http://localhost:8787";

        [Tooltip("拉配置的超时秒数")]
        public int TimeoutSeconds = 5;
    }
}
