using CyberTokyo.Networking;
using UnityEditor;
using UnityEngine;

namespace CyberTokyo.Editor
{
    /// <summary>在 Resources 下生成 GameServerSettings（不存在时），默认指向本地 wrangler dev。</summary>
    public static class NetworkSettingsTool
    {
        private const string AssetPath = "Assets/Resources/Data/GameServerSettings.asset";

        [MenuItem("Tools/Cyber Tokyo/Create Game Server Settings")]
        public static void Create()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameServerSettings>(AssetPath);
            if (existing != null)
            {
                Debug.Log($"[NetworkSettingsTool] 已存在: {AssetPath}（BaseUrl={existing.BaseUrl}），不重建");
                Selection.activeObject = existing;
                return;
            }

            var settings = ScriptableObject.CreateInstance<GameServerSettings>();
            AssetDatabase.CreateAsset(settings, AssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[NetworkSettingsTool] 已创建 {AssetPath}，BaseUrl={settings.BaseUrl}");
            Selection.activeObject = settings;
        }
    }
}
