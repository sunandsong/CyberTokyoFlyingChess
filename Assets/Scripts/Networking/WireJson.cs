using System;
using CyberTokyo.Core.Board;
using Newtonsoft.Json;

namespace CyberTokyo.Networking
{
    /// <summary>
    /// wire JSON ↔ C# DTO 的枚举转换。字段名大小写 Newtonsoft 自己就能对上
    /// （index ↔ Index），但枚举值是后台定义的 wire string（"corner_building"、
    /// "startGate"），必须走 BoardTypes 里那套 ToWire/FromWire，不能用默认的
    /// StringEnumConverter —— 命名风格混着 snake_case 和 camelCase，通用策略罩不住。
    /// </summary>
    public static class WireJson
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Converters =
            {
                new TileColorConverter(),
                new TileKindConverter(),
                new RewardKindConverter(),
                new BuildingIdConverter(),
                new CornerSlotConverter(),
            },
        };

        public static T Deserialize<T>(string json) => JsonConvert.DeserializeObject<T>(json, Settings);

        private class TileColorConverter : JsonConverter<TileColor>
        {
            public override TileColor ReadJson(JsonReader reader, Type objectType, TileColor existingValue, bool hasExistingValue, JsonSerializer serializer)
                => TileColorExtensions.TileColorFromWire((string)reader.Value);

            public override void WriteJson(JsonWriter writer, TileColor value, JsonSerializer serializer)
                => writer.WriteValue(value.ToWire());
        }

        private class TileKindConverter : JsonConverter<TileKind>
        {
            public override TileKind ReadJson(JsonReader reader, Type objectType, TileKind existingValue, bool hasExistingValue, JsonSerializer serializer)
                => TileKindExtensions.TileKindFromWire((string)reader.Value);

            public override void WriteJson(JsonWriter writer, TileKind value, JsonSerializer serializer)
                => writer.WriteValue(value.ToWire());
        }

        private class RewardKindConverter : JsonConverter<RewardKind>
        {
            public override RewardKind ReadJson(JsonReader reader, Type objectType, RewardKind existingValue, bool hasExistingValue, JsonSerializer serializer)
                => RewardKindExtensions.RewardKindFromWire((string)reader.Value);

            public override void WriteJson(JsonWriter writer, RewardKind value, JsonSerializer serializer)
                => writer.WriteValue(value.ToWire());
        }

        private class BuildingIdConverter : JsonConverter<BuildingId>
        {
            public override BuildingId ReadJson(JsonReader reader, Type objectType, BuildingId existingValue, bool hasExistingValue, JsonSerializer serializer)
                => BuildingIdExtensions.BuildingIdFromWire((string)reader.Value);

            public override void WriteJson(JsonWriter writer, BuildingId value, JsonSerializer serializer)
                => writer.WriteValue(value.ToWire());
        }

        private class CornerSlotConverter : JsonConverter<CornerSlot>
        {
            public override CornerSlot ReadJson(JsonReader reader, Type objectType, CornerSlot existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                var wire = (string)reader.Value;
                return wire switch
                {
                    "topLeft" => CornerSlot.TopLeft,
                    "topRight" => CornerSlot.TopRight,
                    "bottomRight" => CornerSlot.BottomRight,
                    "bottomLeft" => CornerSlot.BottomLeft,
                    _ => throw new JsonSerializationException($"unknown CornerSlot wire value: {wire}"),
                };
            }

            public override void WriteJson(JsonWriter writer, CornerSlot value, JsonSerializer serializer)
            {
                writer.WriteValue(value switch
                {
                    CornerSlot.TopLeft => "topLeft",
                    CornerSlot.TopRight => "topRight",
                    CornerSlot.BottomRight => "bottomRight",
                    CornerSlot.BottomLeft => "bottomLeft",
                    _ => throw new JsonSerializationException($"unknown CornerSlot: {value}"),
                });
            }
        }
    }
}
