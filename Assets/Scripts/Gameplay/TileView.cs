using CyberTokyo.Core.Board;
using UnityEngine;

namespace CyberTokyo.Gameplay
{
    /// <summary>
    /// 一格棋盘：有真美术贴图用贴图，没有就纯色占位。带奖励的格子在角上放一个
    /// 奖励标记（有图标用图标，没有用白点）。
    /// 交界格（双色）先只取第一个颜色 —— 形式 1 默认配置里不存在交界格，
    /// 真用上时再决定怎么画对角双色。
    /// </summary>
    public class TileView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        public TileConfigDto Tile { get; private set; }

        private SpriteRenderer _rewardMarker;

        /// <summary>等距视角的画家排序：靠近观察者的格子后画。奖励标记跟着 +1</summary>
        public void SetSortOrder(int order)
        {
            if (spriteRenderer != null) spriteRenderer.sortingOrder = order;
            if (_rewardMarker != null) _rewardMarker.sortingOrder = order + 1;
        }

        public void Initialize(TileConfigDto tile, BoardVisuals visuals)
        {
            Tile = tile;
            if (spriteRenderer == null || tile.Colors.Count == 0) return;

            var color = tile.Colors[0];
            var sprite = visuals.Palette != null ? visuals.Palette.ResolveSprite(color) : null;
            if (sprite != null)
            {
                spriteRenderer.sprite = sprite;
                spriteRenderer.color = Color.white; // 真图不再染色
            }
            else if (visuals.Palette != null)
            {
                spriteRenderer.color = visuals.Palette.Resolve(color);
            }

            RefreshRewardMarker(visuals);
        }

        /// <summary>奖励标记。RewardPlacement 是开局后才写 OnLand 的，所以单独暴露刷新入口</summary>
        public void RefreshRewardMarker(BoardVisuals visuals)
        {
            var reward = Tile.OnLand ?? Tile.OnPass;
            bool hasReward = reward != null;

            if (!hasReward)
            {
                if (_rewardMarker != null) _rewardMarker.gameObject.SetActive(false);
                return;
            }

            if (_rewardMarker == null)
            {
                var markerGo = new GameObject("RewardMarker");
                markerGo.transform.SetParent(transform, false);
                markerGo.transform.localPosition = new Vector3(0.28f, 0.28f, 0f);
                markerGo.transform.localScale = Vector3.one * 0.3f;
                _rewardMarker = markerGo.AddComponent<SpriteRenderer>();
                _rewardMarker.sortingOrder = spriteRenderer.sortingOrder + 1;
            }

            _rewardMarker.gameObject.SetActive(true);

            var icon = visuals.RewardIcons != null ? visuals.RewardIcons.Resolve(reward.Kind) : null;
            if (icon != null)
            {
                _rewardMarker.sprite = icon;
                _rewardMarker.color = Color.white;
            }
            else
            {
                // 没有图标素材：借用格子自己的贴图形状当占位（缩小的白点）
                _rewardMarker.sprite = spriteRenderer.sprite;
                _rewardMarker.color = Color.white;
            }
        }
    }
}
