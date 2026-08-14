using System;
using System.Collections.Generic;

namespace CyberTokyo.Core.Board
{
    /// <summary>
    /// 下发配置的形状 —— 移植自 fly-game-admin/src/types.ts。
    /// 字段名/取值刻意跟后台的 wire string 完全一致（见各枚举的 ToWire/FromWire），
    /// 不建第二套翻译表，JSON 往返（Phase 4）和文件命名（Phase 5）都直接复用这份映射。
    /// </summary>
    public enum TileColor
    {
        Green,
        Yellow,
        Red,
        Blue,
    }

    public static class TileColorExtensions
    {
        public static string ToWire(this TileColor c) => c switch
        {
            TileColor.Green => "green",
            TileColor.Yellow => "yellow",
            TileColor.Red => "red",
            TileColor.Blue => "blue",
            _ => throw new ArgumentOutOfRangeException(nameof(c), c, null),
        };

        public static TileColor TileColorFromWire(string wire) => wire switch
        {
            "green" => TileColor.Green,
            "yellow" => TileColor.Yellow,
            "red" => TileColor.Red,
            "blue" => TileColor.Blue,
            _ => throw new ArgumentException($"unknown TileColor wire value: {wire}"),
        };
    }

    public enum TileKind
    {
        Normal,
        CornerBuilding,
        ConveyorTrigger,
        FreeTeleport,
        Junction,
    }

    public static class TileKindExtensions
    {
        public static string ToWire(this TileKind k) => k switch
        {
            TileKind.Normal => "normal",
            TileKind.CornerBuilding => "corner_building",
            TileKind.ConveyorTrigger => "conveyor_trigger",
            TileKind.FreeTeleport => "free_teleport",
            TileKind.Junction => "junction",
            _ => throw new ArgumentOutOfRangeException(nameof(k), k, null),
        };

        public static TileKind TileKindFromWire(string wire) => wire switch
        {
            "normal" => TileKind.Normal,
            "corner_building" => TileKind.CornerBuilding,
            "conveyor_trigger" => TileKind.ConveyorTrigger,
            "free_teleport" => TileKind.FreeTeleport,
            "junction" => TileKind.Junction,
            _ => throw new ArgumentException($"unknown TileKind wire value: {wire}"),
        };
    }

    public enum RewardKind
    {
        Coin,
        Banknote,
        Dice,
        CardShard,
        Mystery,
    }

    public static class RewardKindExtensions
    {
        public static string ToWire(this RewardKind k) => k switch
        {
            RewardKind.Coin => "coin",
            RewardKind.Banknote => "banknote",
            RewardKind.Dice => "dice",
            RewardKind.CardShard => "card_shard",
            RewardKind.Mystery => "mystery",
            _ => throw new ArgumentOutOfRangeException(nameof(k), k, null),
        };

        public static RewardKind RewardKindFromWire(string wire) => wire switch
        {
            "coin" => RewardKind.Coin,
            "banknote" => RewardKind.Banknote,
            "dice" => RewardKind.Dice,
            "card_shard" => RewardKind.CardShard,
            "mystery" => RewardKind.Mystery,
            _ => throw new ArgumentException($"unknown RewardKind wire value: {wire}"),
        };
    }

    /// <summary>
    /// 四角建筑的素材 id。取自客户端 atlas/sucaiRegions.ts 的 CORNER_BUILDINGS，
    /// 名字必须与后台一致。
    /// </summary>
    public enum BuildingId
    {
        StartGate,
        TokyoTower,
        SensojiPagoda,
        LuckyCatShrine,
    }

    public static class BuildingIdExtensions
    {
        public static string ToWire(this BuildingId b) => b switch
        {
            BuildingId.StartGate => "startGate",
            BuildingId.TokyoTower => "tokyoTower",
            BuildingId.SensojiPagoda => "sensojiPagoda",
            BuildingId.LuckyCatShrine => "luckyCatShrine",
            _ => throw new ArgumentOutOfRangeException(nameof(b), b, null),
        };

        public static BuildingId BuildingIdFromWire(string wire) => wire switch
        {
            "startGate" => BuildingId.StartGate,
            "tokyoTower" => BuildingId.TokyoTower,
            "sensojiPagoda" => BuildingId.SensojiPagoda,
            "luckyCatShrine" => BuildingId.LuckyCatShrine,
            _ => throw new ArgumentException($"unknown BuildingId wire value: {wire}"),
        };
    }

    /// <summary>一份奖励：给什么、给多少。恒为正整数 —— "没有奖励"是整个字段不填（null），不是 Amount:0</summary>
    [Serializable]
    public class RewardAmountDto
    {
        public RewardKind Kind;
        public int Amount;
    }

    [Serializable]
    public class TileConfigDto
    {
        /// <summary>环路序号，0 起</summary>
        public int Index;
        public TileKind Kind;
        /// <summary>单色格 1 项；交界格 2 项（对角双色）</summary>
        public List<TileColor> Colors = new List<TileColor>();
        /// <summary>经过就拿到的奖励。与 OnLand 互不影响，同一格可以两种都有。null = 没有</summary>
        public RewardAmountDto OnPass;
        /// <summary>必须正好踩上才拿到。null = 没有</summary>
        public RewardAmountDto OnLand;
    }

    [Serializable]
    public class ConveyorConfigDto
    {
        public TileColor Color;
        /// <summary>触发它的那格在环路上的序号</summary>
        public int TriggerTileIndex;
        /// <summary>铺几格。逻辑距离是这个数 + 1，中心自己算一格</summary>
        public int Length;
    }

    /// <summary>一块角落放哪栋建筑。建筑不在环路格上 —— 占的是十字缺掉的那块 3x3 空地</summary>
    [Serializable]
    public class CornerConfigDto
    {
        public CornerSlot Slot;
        public BuildingId Building;
    }

    /// <summary>
    /// 中心的一个状态。每张图代表一个状态，同一时间只显示一张。
    /// AssetId 为 null 时允许先把状态列出来、图后补。
    /// </summary>
    [Serializable]
    public class CenterStateDto
    {
        public string Key;
        public string AssetId;
    }

    [Serializable]
    public class CenterConfigDto
    {
        public List<CenterStateDto> States = new List<CenterStateDto>();
    }

    [Serializable]
    public class BoardConfigDto
    {
        /// <summary>形式 1 = 36 普通 + 12 特殊；形式 2 = 40 普通 + 8 特殊，内拐角放交界格</summary>
        public string Form;
        public List<TileConfigDto> Tiles = new List<TileConfigDto>();
        public List<ConveyorConfigDto> Conveyors = new List<ConveyorConfigDto>();
        /// <summary>四块角落各放一栋建筑。四栋各用一次</summary>
        public List<CornerConfigDto> Corners = new List<CornerConfigDto>();
        public CenterConfigDto Center = new CenterConfigDto();
    }
}
