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

        /// <summary>
        /// 按 art-spec 的命名约定自动把 Assets/Art/Sprites/ 下的图挂进查找表：
        /// center_&lt;key&gt; / tile_&lt;color&gt; / piece_&lt;color&gt; / building_&lt;id&gt; / reward_&lt;kind&gt;。
        /// 找不到的条目保持原样（继续用占位），可反复跑。
        /// </summary>
        [MenuItem("Tools/Cyber Tokyo/Auto-Wire Art From Sprites")]
        public static void AutoWire()
        {
            int wired = 0;

            var center = AssetDatabase.LoadAssetAtPath<CenterStateVisualSO>("Assets/Resources/Data/CenterStateVisuals.asset");
            if (center != null)
            {
                for (int i = 0; i < center.Entries.Length; i++)
                {
                    var sprite = LoadSprite($"Assets/Art/Sprites/Center/center_{center.Entries[i].StateKey}.png");
                    if (sprite != null) { center.Entries[i].Sprite = sprite; wired++; }
                }
                EditorUtility.SetDirty(center);
            }

            var palette = AssetDatabase.LoadAssetAtPath<TileColorPaletteSO>("Assets/Resources/Data/TileColorPalette.asset");
            if (palette != null)
            {
                for (int i = 0; i < palette.Entries.Length; i++)
                {
                    var sprite = LoadSprite($"Assets/Art/Sprites/Board/tile_{palette.Entries[i].Color.ToWire()}.png");
                    if (sprite != null) { palette.Entries[i].TileSprite = sprite; wired++; }
                }
                EditorUtility.SetDirty(palette);
            }

            var pieces = AssetDatabase.LoadAssetAtPath<PieceVisualSO>("Assets/Resources/Data/PieceVisuals.asset");
            if (pieces != null)
            {
                for (int i = 0; i < pieces.Entries.Length; i++)
                {
                    var sprite = LoadSprite($"Assets/Art/Sprites/Pieces/piece_{pieces.Entries[i].Color.ToWire()}.png");
                    if (sprite != null) { pieces.Entries[i].Sprite = sprite; wired++; }
                }
                EditorUtility.SetDirty(pieces);
            }

            var buildings = AssetDatabase.LoadAssetAtPath<BuildingVisualSO>("Assets/Resources/Data/BuildingVisuals.asset");
            if (buildings != null)
            {
                for (int i = 0; i < buildings.Entries.Length; i++)
                {
                    var sprite = LoadSprite($"Assets/Art/Sprites/Buildings/building_{buildings.Entries[i].Building.ToWire()}.png");
                    if (sprite != null) { buildings.Entries[i].Sprite = sprite; wired++; }
                }
                EditorUtility.SetDirty(buildings);
            }

            var rewards = AssetDatabase.LoadAssetAtPath<RewardIconSO>("Assets/Resources/Data/RewardIcons.asset");
            if (rewards != null)
            {
                for (int i = 0; i < rewards.Entries.Length; i++)
                {
                    var sprite = LoadSprite($"Assets/Art/Sprites/Reward/reward_{rewards.Entries[i].Kind.ToWire()}.png");
                    if (sprite != null) { rewards.Entries[i].Sprite = sprite; wired++; }
                }
                EditorUtility.SetDirty(rewards);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[ArtVisualsTool] Auto-wire 完成，挂上 {wired} 张图");
        }

        private static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

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
