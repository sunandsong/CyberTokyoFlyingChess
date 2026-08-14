using System.Collections.Generic;
using System.IO;
using System.Linq;
using CyberTokyo.Core;
using CyberTokyo.Core.Board;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace CyberTokyo.Tests.EditMode
{
    /// <summary>
    /// 两类测试：
    /// 1. 内部不变量 —— 镜像 fly-game-admin/src/validate.ts 校验的那批规则
    ///    （48 格、相邻不同色、四色各 12、特殊格间距 12），只在客户端生成的默认配置上跑。
    /// 2. 对账测试 —— 跟后台 README 里说的"拉 /api/game/config 跟客户端输出逐字节比"
    ///    是同一套办法的客户端镜像版：diff DefaultConfigFactory 的输出和一份从
    ///    wrangler dev 拉下来、原样存进 Fixtures/sample-config.json 的快照。
    ///    geometry.ts/types.ts/defaults.ts 有改动时，重新 curl 一份覆盖这个 fixture，
    ///    这个测试跑一遍就知道两边有没有跟着变。
    /// </summary>
    public class BoardGeometryParityTests
    {
        private const string FixtureRelativePath = "Tests/Fixtures/sample-config.json";

        private static BoardConfigDto DefaultBoard => DefaultConfigFactory.CreateDefaultBoardConfig();

        // ── 内部不变量 ──────────────────────────────────────────────

        [Test]
        public void DefaultBoard_HasFortyEightTiles()
        {
            Assert.AreEqual(BoardGeometry.RingTileCount, DefaultBoard.Tiles.Count);
        }

        [Test]
        public void DefaultBoard_NoAdjacentSameColor_IncludingWraparound()
        {
            var tiles = DefaultBoard.Tiles;
            for (int i = 0; i < tiles.Count; i++)
            {
                var current = tiles[i];
                var next = tiles[(i + 1) % tiles.Count];

                // 交界格是对角双色，跟它比较时用贴邻的那一侧颜色（最后一项）
                TileColor currentOutward = current.Colors[current.Colors.Count - 1];
                TileColor nextInward = next.Colors[0];

                Assert.AreNotEqual(currentOutward, nextInward,
                    $"tile {current.Index} and tile {next.Index} are adjacent and same color");
            }
        }

        [Test]
        public void DefaultBoard_EachColorAppearsTwelveTimes()
        {
            var counts = new Dictionary<TileColor, int>();
            foreach (var tile in DefaultBoard.Tiles)
            {
                foreach (var color in tile.Colors)
                {
                    counts.TryGetValue(color, out int c);
                    counts[color] = c + 1;
                }
            }

            foreach (TileColor color in new[] { TileColor.Green, TileColor.Yellow, TileColor.Red, TileColor.Blue })
            {
                Assert.AreEqual(12, counts.GetValueOrDefault(color, 0), $"color {color} should appear 12 times");
            }
        }

        [Test]
        public void DefaultBoard_SpecialTileCounts_MatchForm1()
        {
            var byKind = DefaultBoard.Tiles.GroupBy(t => t.Kind).ToDictionary(g => g.Key, g => g.Count());

            Assert.AreEqual(4, byKind.GetValueOrDefault(TileKind.CornerBuilding, 0));
            Assert.AreEqual(4, byKind.GetValueOrDefault(TileKind.ConveyorTrigger, 0));
            Assert.AreEqual(4, byKind.GetValueOrDefault(TileKind.FreeTeleport, 0));
            Assert.AreEqual(36, byKind.GetValueOrDefault(TileKind.Normal, 0));
        }

        [TestCase(TileKind.CornerBuilding)]
        [TestCase(TileKind.ConveyorTrigger)]
        [TestCase(TileKind.FreeTeleport)]
        public void DefaultBoard_SpecialTiles_AreQuarterSpaced(TileKind kind)
        {
            var indices = DefaultBoard.Tiles.Where(t => t.Kind == kind).Select(t => t.Index).OrderBy(i => i).ToList();
            Assert.AreEqual(4, indices.Count);

            for (int i = 1; i < indices.Count; i++)
            {
                Assert.AreEqual(12, indices[i] - indices[i - 1], $"{kind} tiles should be 12 apart");
            }
        }

        [Test]
        public void DefaultBoard_ConveyorTriggers_AreAtArmMidpoints()
        {
            foreach (var conveyor in DefaultBoard.Conveyors)
            {
                var pos = BoardGeometry.RingPosition(conveyor.TriggerTileIndex);
                Assert.IsTrue(BoardGeometry.IsArmMidpoint(pos),
                    $"conveyor trigger at index {conveyor.TriggerTileIndex} is not at an arm midpoint — its conveyor path would run diagonally instead of straight to center");
            }
        }

        [Test]
        public void RingPosition_And_RingIndexAt_RoundTripForAllTiles()
        {
            for (int i = 0; i < BoardGeometry.RingTileCount; i++)
            {
                var pos = BoardGeometry.RingPosition(i);
                Assert.AreEqual(i, BoardGeometry.RingIndexAt(pos), $"round-trip failed at index {i}");
            }
        }

        // ── 对账：跟后台实时快照比 ──────────────────────────────────

        [Test]
        public void DefaultBoard_MatchesBackendSnapshot()
        {
            string fixturePath = Path.Combine(Application.dataPath, FixtureRelativePath);
            if (!File.Exists(fixturePath))
            {
                Assert.Ignore(
                    "没有找到对账用的快照。先在 fly-game-admin 跑 `npm run dev`，再执行：\n" +
                    $"  curl -s http://localhost:8787/api/game/config > {fixturePath}\n" +
                    "这个测试才会真的去对账，不是必须每次都跑但改动 geometry/types/defaults 之后应该重新对一次。");
                return;
            }

            var snapshot = JObject.Parse(File.ReadAllText(fixturePath));
            var remoteTiles = (JArray)snapshot["board"]["data"]["tiles"];
            var localTiles = DefaultBoard.Tiles;

            Assert.AreEqual(localTiles.Count, remoteTiles.Count, "tile count mismatch");

            for (int i = 0; i < localTiles.Count; i++)
            {
                var remote = remoteTiles[i];
                var local = localTiles[i];

                Assert.AreEqual(local.Index, (int)remote["index"], $"tile {i} index mismatch");
                Assert.AreEqual(local.Kind.ToWire(), (string)remote["kind"], $"tile {i} kind mismatch");

                var remoteColors = ((JArray)remote["colors"]).Select(c => (string)c).ToList();
                var localColors = local.Colors.Select(c => c.ToWire()).ToList();
                CollectionAssert.AreEqual(remoteColors, localColors, $"tile {i} colors mismatch");
            }

            var remoteConveyors = (JArray)snapshot["board"]["data"]["conveyors"];
            Assert.AreEqual(DefaultBoard.Conveyors.Count, remoteConveyors.Count, "conveyor count mismatch");
            for (int i = 0; i < DefaultBoard.Conveyors.Count; i++)
            {
                var remote = remoteConveyors[i];
                var local = DefaultBoard.Conveyors[i];
                Assert.AreEqual(local.TriggerTileIndex, (int)remote["triggerTileIndex"], $"conveyor {i} trigger mismatch");
                Assert.AreEqual(local.Color.ToWire(), (string)remote["color"], $"conveyor {i} color mismatch");
                Assert.AreEqual(local.Length, (int)remote["length"], $"conveyor {i} length mismatch");
            }

            var remoteCorners = (JArray)snapshot["board"]["data"]["corners"];
            Assert.AreEqual(DefaultBoard.Corners.Count, remoteCorners.Count, "corner count mismatch");
        }
    }
}
