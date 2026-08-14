using CyberTokyo.Core;
using UnityEditor;
using UnityEngine;

namespace CyberTokyo.Editor
{
    /// <summary>
    /// 生成/覆盖 Assets/Data/DefaultGameConfig.asset。跑 DefaultConfigFactory 的真实算法
    /// 而不是手抄一份数字，避免跟 fly-game-admin/src/defaults.ts 产生它本想避免的那种漂移。
    ///
    /// 也可以走命令行批处理跑：
    ///   Unity -batchmode -quit -projectPath &lt;path&gt; \
    ///     -executeMethod CyberTokyo.Editor.DefaultConfigGenerator.Generate
    /// </summary>
    public static class DefaultConfigGenerator
    {
        private const string AssetPath = "Assets/Data/DefaultGameConfig.asset";

        [MenuItem("Tools/Cyber Tokyo/Regenerate Default Config")]
        public static void Generate()
        {
            var asset = AssetDatabase.LoadAssetAtPath<DefaultGameConfigAsset>(AssetPath);
            bool isNew = asset == null;
            if (isNew)
            {
                asset = ScriptableObject.CreateInstance<DefaultGameConfigAsset>();
            }

            asset.Board = DefaultConfigFactory.CreateDefaultBoardConfig();
            asset.Reward = DefaultConfigFactory.CreateDefaultRewardConfig();

            if (isNew)
            {
                AssetDatabase.CreateAsset(asset, AssetPath);
            }
            else
            {
                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[DefaultConfigGenerator] wrote {asset.Board.Tiles.Count} tiles, " +
                      $"{asset.Board.Conveyors.Count} conveyors to {AssetPath}");
        }
    }
}
