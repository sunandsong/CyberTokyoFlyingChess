using CyberTokyo.Core.Board;
using CyberTokyo.Gameplay;
using UnityEditor;
using UnityEngine;

namespace CyberTokyo.Editor
{
    /// <summary>
    /// 一键创建四张美术查找表（建筑/中心状态/奖励图标/棋子），条目预填好、图先空着。
    /// 素材做出来后在 Inspector 里把图拖进对应条目即可，代码不用动。已存在的不覆盖。
    /// </summary>
    public static class ArtVisualsTool
    {
        [MenuItem("Tools/Cyber Tokyo/Create Art Visual Assets")]
        public static void Create()
        {
            EnsureBuildingVisuals();
            EnsureCenterStateVisuals();
            EnsureRewardIcons();
            EnsurePieceVisuals();
            AssetDatabase.SaveAssets();
            Debug.Log("[ArtVisualsTool] 美术查找表就绪（Assets/Resources/Data/），条目已预填，等着拖图");
        }

        private static void EnsureBuildingVisuals()
        {
            const string path = "Assets/Resources/Data/BuildingVisuals.asset";
            if (AssetDatabase.LoadAssetAtPath<BuildingVisualSO>(path) != null) return;

            var so = ScriptableObject.CreateInstance<BuildingVisualSO>();
            so.Entries = new[]
            {
                new BuildingVisualSO.Entry { Building = BuildingId.StartGate },
                new BuildingVisualSO.Entry { Building = BuildingId.TokyoTower },
                new BuildingVisualSO.Entry { Building = BuildingId.SensojiPagoda },
                new BuildingVisualSO.Entry { Building = BuildingId.LuckyCatShrine },
            };
            AssetDatabase.CreateAsset(so, path);
        }

        private static void EnsureCenterStateVisuals()
        {
            const string path = "Assets/Resources/Data/CenterStateVisuals.asset";
            if (AssetDatabase.LoadAssetAtPath<CenterStateVisualSO>(path) != null) return;

            var so = ScriptableObject.CreateInstance<CenterStateVisualSO>();
            so.Entries = new[]
            {
                new CenterStateVisualSO.Entry { StateKey = "sleeping" },
                new CenterStateVisualSO.Entry { StateKey = "angry" },
                new CenterStateVisualSO.Entry { StateKey = "atomicBreath" },
                new CenterStateVisualSO.Entry { StateKey = "pleased" },
            };
            AssetDatabase.CreateAsset(so, path);
        }

        private static void EnsureRewardIcons()
        {
            const string path = "Assets/Resources/Data/RewardIcons.asset";
            if (AssetDatabase.LoadAssetAtPath<RewardIconSO>(path) != null) return;

            var so = ScriptableObject.CreateInstance<RewardIconSO>();
            so.Entries = new[]
            {
                new RewardIconSO.Entry { Kind = RewardKind.Coin },
                new RewardIconSO.Entry { Kind = RewardKind.Banknote },
                new RewardIconSO.Entry { Kind = RewardKind.Dice },
                new RewardIconSO.Entry { Kind = RewardKind.CardShard },
                new RewardIconSO.Entry { Kind = RewardKind.Mystery },
            };
            AssetDatabase.CreateAsset(so, path);
        }

        private static void EnsurePieceVisuals()
        {
            const string path = "Assets/Resources/Data/PieceVisuals.asset";
            if (AssetDatabase.LoadAssetAtPath<PieceVisualSO>(path) != null) return;

            var so = ScriptableObject.CreateInstance<PieceVisualSO>();
            so.Entries = new[]
            {
                new PieceVisualSO.Entry { Color = TileColor.Green },
                new PieceVisualSO.Entry { Color = TileColor.Yellow },
                new PieceVisualSO.Entry { Color = TileColor.Red },
                new PieceVisualSO.Entry { Color = TileColor.Blue },
            };
            AssetDatabase.CreateAsset(so, path);
        }
    }
}
