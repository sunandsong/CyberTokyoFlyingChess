using System.Collections.Generic;
using CyberTokyo.Core.Board;
using CyberTokyo.Core.Reward;

namespace CyberTokyo.Core
{
    /// <summary>
    /// 第 1 版配置 —— 移植自 fly-game-admin/src/defaults.ts，逐条对应，刻意保持算法一致
    /// 而不是照抄一份数字快照，这样后台改算法时能一眼看出这边要不要跟着改。
    ///
    /// 用途：Assets/Scripts/Editor/DefaultConfigGenerator.cs 用它生成
    /// Assets/Data/DefaultGameConfig.asset（离线兜底配置）；
    /// Assets/Tests/EditMode/BoardGeometryParityTests.cs 用它做对账测试的基准之一。
    /// </summary>
    public static class DefaultConfigFactory
    {
        /// <summary>四角建筑：东京塔 / 浅草寺塔 / 招财猫神社 / スタート牌坊。落在外凸角</summary>
        private static readonly int[] CornerBuildingIndices = { 0, 12, 24, 36 };

        /// <summary>
        /// 哪个角放哪栋楼。只有 0 号格旁边那块是有依据的（スタート牌坊紧邻出生点），
        /// 另外三栋是占位方案，后台可以点着改。
        /// </summary>
        private static readonly BuildingId[] DefaultBuildings =
        {
            BuildingId.StartGate,
            BuildingId.TokyoTower,
            BuildingId.SensojiPagoda,
            BuildingId.LuckyCatShrine,
        };

        /// <summary>箭头格，落在四条臂的正中</summary>
        private static readonly int[] ConveyorTriggerIndices = { 3, 15, 27, 39 };

        /// <summary>自由传送格，形式 1 专有，落在四个内拐角</summary>
        private static readonly int[] FreeTeleportIndices = { 9, 21, 33, 45 };

        /// <summary>
        /// 四条路各自的颜色，与 ConveyorTriggerIndices 一一对应。取自拓扑图实测，
        /// 刻意不从配色循环推导（四个箭头格等距 12 格，跟着循环走会全同色）。
        /// </summary>
        private static readonly TileColor[] ConveyorColors =
        {
            TileColor.Green,
            TileColor.Blue,
            TileColor.Red,
            TileColor.Yellow,
        };

        private static readonly TileColor[] TileColorCycle =
        {
            TileColor.Green,
            TileColor.Yellow,
            TileColor.Red,
            TileColor.Blue,
        };

        private static readonly string[] SuggestedCenterStates =
        {
            "sleeping",
            "angry",
            "atomicBreath",
            "pleased",
        };

        private static int IndexOf(int[] arr, int value)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] == value) return i;
            }
            return -1;
        }

        private static bool Contains(int[] arr, int value) => IndexOf(arr, value) >= 0;

        private static TileKind KindAt(int index)
        {
            if (Contains(CornerBuildingIndices, index)) return TileKind.CornerBuilding;
            if (Contains(ConveyorTriggerIndices, index)) return TileKind.ConveyorTrigger;
            if (Contains(FreeTeleportIndices, index)) return TileKind.FreeTeleport;
            return TileKind.Normal;
        }

        /// <summary>
        /// 这一类格子在配色循环上占几个位置。箭头格占 0 个（颜色是路的属性，不参与循环），
        /// 交界格占 2 个（对角拼接两种循环色）。
        /// </summary>
        private static int CycleSlotsTaken(TileKind kind)
        {
            if (kind == TileKind.ConveyorTrigger) return 0;
            if (kind == TileKind.Junction) return 2;
            return 1;
        }

        private static TileColor ColorAt(int cycleSlot)
        {
            int n = TileColorCycle.Length;
            int i = ((cycleSlot % n) + n) % n;
            return TileColorCycle[i];
        }

        public static BoardConfigDto CreateDefaultBoardConfig()
        {
            var tiles = new List<TileConfigDto>(BoardGeometry.RingTileCount);
            int cycleSlot = 0;

            for (int index = 0; index < BoardGeometry.RingTileCount; index++)
            {
                TileKind kind = KindAt(index);

                var colors = new List<TileColor>();
                if (kind == TileKind.ConveyorTrigger)
                {
                    int slot = IndexOf(ConveyorTriggerIndices, index);
                    colors.Add(ConveyorColors[slot]);
                }
                else if (kind == TileKind.Junction)
                {
                    colors.Add(ColorAt(cycleSlot));
                    colors.Add(ColorAt(cycleSlot + 1));
                }
                else
                {
                    colors.Add(ColorAt(cycleSlot));
                }

                tiles.Add(new TileConfigDto { Index = index, Kind = kind, Colors = colors });
                cycleSlot += CycleSlotsTaken(kind);
            }

            var corners = new List<CornerConfigDto>(CornerBuildingIndices.Length);
            for (int slot = 0; slot < CornerBuildingIndices.Length; slot++)
            {
                int index = CornerBuildingIndices[slot];
                corners.Add(new CornerConfigDto
                {
                    Slot = BoardGeometry.CornerSlotNearTile(index),
                    Building = DefaultBuildings[slot],
                });
            }

            var centerStates = new List<CenterStateDto>(SuggestedCenterStates.Length);
            foreach (var key in SuggestedCenterStates)
            {
                centerStates.Add(new CenterStateDto { Key = key, AssetId = null });
            }

            var conveyors = new List<ConveyorConfigDto>(ConveyorTriggerIndices.Length);
            for (int slot = 0; slot < ConveyorTriggerIndices.Length; slot++)
            {
                conveyors.Add(new ConveyorConfigDto
                {
                    Color = ConveyorColors[slot],
                    TriggerTileIndex = ConveyorTriggerIndices[slot],
                    Length = BoardGeometry.ConveyorLength,
                });
            }

            return new BoardConfigDto
            {
                Form = "form1",
                Corners = corners,
                Center = new CenterConfigDto { States = centerStates },
                Tiles = tiles,
                Conveyors = conveyors,
            };
        }

        /// <summary>
        /// 第 1 版奖励配置，数值全部是占位值（设计文档没给数字），估算依据见后台
        /// src/defaults.ts 的注释。改这些数不需要发版 —— 但客户端这份是离线兜底，
        /// 改了之后照样得跟着 Phase 4 的 DefaultConfigGenerator 重新生成一次。
        /// </summary>
        public static RewardConfigDto CreateDefaultRewardConfig()
        {
            return new RewardConfigDto
            {
                ValueScale = new LinearCurveDto { Base = 1f, PerLevel = 0.5f },
                Density = new LinearCurveDto { Base = 6f, PerLevel = 2f },
                Pool = new List<RewardPoolEntryDto>
                {
                    new RewardPoolEntryDto { Kind = RewardKind.Coin, Weight = 60, UnlockLevel = 1 },
                    new RewardPoolEntryDto { Kind = RewardKind.Dice, Weight = 15, UnlockLevel = 1 },
                    new RewardPoolEntryDto { Kind = RewardKind.CardShard, Weight = 15, UnlockLevel = 1 },
                    new RewardPoolEntryDto { Kind = RewardKind.Mystery, Weight = 10, UnlockLevel = 1 },
                    new RewardPoolEntryDto { Kind = RewardKind.Banknote, Weight = 30, UnlockLevel = 5 },
                },
                ConveyorEndReward = new RewardAmountDto { Kind = RewardKind.Dice, Amount = 1 },
            };
        }
    }
}
