using System;
using System.Collections;
using System.IO;
using CyberTokyo.Core;
using CyberTokyo.Core.Board;
using CyberTokyo.Core.Reward;
using UnityEngine;

namespace CyberTokyo.Networking
{
    /// <summary>本局最终采用的配置，以及它是从哪来的（网络/缓存/内置）</summary>
    public class LoadedConfig
    {
        public BoardConfigDto Board;
        public RewardConfigDto Reward;
        public int BoardVersion;
        public int RewardVersion;
        /// <summary>"network" / "cache" / "builtin"</summary>
        public string Source;
    }

    /// <summary>
    /// 三级兜底：网络 → 上次成功的缓存 → 内置默认配置。
    /// 后台 README 明确要求客户端"拉不到时不能白屏"，这里就是那条要求的实现。
    /// 缓存是拉到的原始 JSON 原样落盘，读的时候走同一套解析，不另存一种格式。
    /// </summary>
    public static class ConfigRepository
    {
        private static string CachePath => Path.Combine(Application.persistentDataPath, "config-cache.json");

        public static IEnumerator Load(GameServerSettings settings, DefaultGameConfigAsset builtin, Action<LoadedConfig> onDone)
        {
            // 1. 网络
            if (settings != null && !string.IsNullOrEmpty(settings.BaseUrl))
            {
                var result = new GameConfigService.Result();
                yield return GameConfigService.Fetch(settings, result);

                if (result.Ok)
                {
                    TryWriteCache(result.RawJson);
                    onDone(FromEnvelope(result.Envelope, "network"));
                    yield break;
                }

                Debug.LogWarning($"[ConfigRepository] 网络拉取失败，走兜底: {result.Error}");
            }

            // 2. 缓存
            var cached = TryReadCache();
            if (cached != null)
            {
                onDone(FromEnvelope(cached, "cache"));
                yield break;
            }

            // 3. 内置
            onDone(new LoadedConfig
            {
                Board = builtin.Board,
                Reward = builtin.Reward,
                BoardVersion = -1,
                RewardVersion = -1,
                Source = "builtin",
            });
        }

        private static LoadedConfig FromEnvelope(GameConfigEnvelope envelope, string source) => new LoadedConfig
        {
            Board = envelope.Board.Data,
            Reward = envelope.Reward.Data,
            BoardVersion = envelope.Board.Version,
            RewardVersion = envelope.Reward.Version,
            Source = source,
        };

        private static void TryWriteCache(string rawJson)
        {
            try
            {
                File.WriteAllText(CachePath, rawJson);
            }
            catch (Exception e)
            {
                // 写缓存失败不影响本局 —— 顶多下次离线时少一级兜底
                Debug.LogWarning($"[ConfigRepository] 写缓存失败: {e.Message}");
            }
        }

        private static GameConfigEnvelope TryReadCache()
        {
            try
            {
                if (!File.Exists(CachePath)) return null;
                var envelope = WireJson.Deserialize<GameConfigEnvelope>(File.ReadAllText(CachePath));
                if (envelope?.Board?.Data?.Tiles == null ||
                    envelope.Board.Data.Tiles.Count != BoardGeometry.RingTileCount)
                {
                    return null;
                }
                return envelope;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ConfigRepository] 读缓存失败: {e.Message}");
                return null;
            }
        }
    }
}
