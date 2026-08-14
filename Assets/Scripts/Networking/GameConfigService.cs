using System;
using System.Collections;
using CyberTokyo.Core.Board;
using CyberTokyo.Core.Reward;
using UnityEngine;
using UnityEngine.Networking;

namespace CyberTokyo.Networking
{
    /// <summary>GET /api/game/config 的响应形状，与 fly-game-admin/src/index.ts 对应</summary>
    [Serializable]
    public class GameConfigEnvelope
    {
        public VersionedBoard Board;
        public VersionedReward Reward;

        [Serializable]
        public class VersionedBoard
        {
            public int Version;
            public BoardConfigDto Data;
        }

        [Serializable]
        public class VersionedReward
        {
            public int Version;
            public RewardConfigDto Data;
        }
    }

    /// <summary>拉取并解析 /api/game/config。只管网络与解析，兜底策略在 ConfigRepository。</summary>
    public static class GameConfigService
    {
        public class Result
        {
            public GameConfigEnvelope Envelope;
            public string RawJson;
            public string Error;
            public bool Ok => Error == null;
        }

        public static IEnumerator Fetch(GameServerSettings settings, Result result)
        {
            string url = settings.BaseUrl.TrimEnd('/') + "/api/game/config";

            using var request = UnityWebRequest.Get(url);
            request.timeout = settings.TimeoutSeconds;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                result.Error = $"{request.result}: {request.error} ({url})";
                yield break;
            }

            try
            {
                result.RawJson = request.downloadHandler.text;
                result.Envelope = WireJson.Deserialize<GameConfigEnvelope>(result.RawJson);

                if (result.Envelope?.Board?.Data?.Tiles == null ||
                    result.Envelope.Board.Data.Tiles.Count != BoardGeometry.RingTileCount)
                {
                    result.Envelope = null;
                    result.Error = "解析成功但棋盘数据不完整（tiles 数量不对）";
                }
            }
            catch (Exception e)
            {
                result.Envelope = null;
                result.Error = $"解析失败: {e.Message}";
            }
        }
    }
}
