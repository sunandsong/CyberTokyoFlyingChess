using System.Collections.Generic;
using CyberTokyo.Core.Board;
using UnityEngine;

namespace CyberTokyo.Gameplay
{
    /// <summary>
    /// 从 BoardConfigDto 摆出 48 格环路 + 4 角建筑 + 中心。Phase 3 是正俯视平铺
    /// （1 格 = 1 世界单位），等距视角是 Phase 7 才切换的表现层改动，这里先不管。
    /// </summary>
    public class BoardRenderer : MonoBehaviour
    {
        // ⚠️ 这几个引用刻意不做成 [SerializeField]：场景对 Gameplay 程序集类型的
        // "资产"引用（prefab 上的组件、SO）在这个项目里存场景时会被静默置空
        // （场景内部引用和 Core 程序集的 SO 不受影响，实测踩过）。
        // 所以走 GameLoopController 在运行时 Resources.Load 后调 Initialize 注入。
        private TileView _tilePrefab;
        private SpriteRenderer _cornerBuildingPrefab;
        private CenterGodzillaController _centerPrefab;
        private BoardVisuals _visuals;

        private readonly Dictionary<int, TileView> _spawnedTiles = new Dictionary<int, TileView>();
        private CenterGodzillaController _centerInstance;

        public void Initialize(TileView tilePrefab, SpriteRenderer cornerBuildingPrefab,
            CenterGodzillaController centerPrefab, BoardVisuals visuals)
        {
            _tilePrefab = tilePrefab;
            _cornerBuildingPrefab = cornerBuildingPrefab;
            _centerPrefab = centerPrefab;
            _visuals = visuals;
        }

        /// <summary>RewardPlacement 开局后才往 tile 上写奖励，写完调这个刷新格子上的标记</summary>
        public void RefreshRewardMarkers()
        {
            foreach (var tile in _spawnedTiles.Values)
            {
                tile.RefreshRewardMarker(_visuals);
            }
        }

        public CenterGodzillaController CenterInstance => _centerInstance;

        /// <summary>格子坐标 -&gt; 世界坐标（等距投影，见 IsoProjection）</summary>
        public static Vector3 WorldPosition(GridPos pos)
        {
            return IsoProjection.WorldPosition(pos);
        }

        public Vector3 WorldPositionForRingIndex(int ringIndex)
        {
            return WorldPosition(BoardGeometry.RingPosition(ringIndex));
        }

        public void Render(BoardConfigDto board)
        {
            Clear();

            foreach (var tile in board.Tiles)
            {
                var pos = BoardGeometry.RingPosition(tile.Index);
                var instance = Instantiate(_tilePrefab, WorldPosition(pos), Quaternion.identity, transform);
                instance.name = $"Tile_{tile.Index:D2}_{tile.Kind}";
                instance.Initialize(tile, _visuals);
                instance.SetSortOrder(IsoProjection.SortOrder(pos));
                if (tile.Kind == TileKind.ConveyorTrigger)
                {
                    instance.gameObject.AddComponent<TilePulse>();
                }
                _spawnedTiles[tile.Index] = instance;
            }

            foreach (var corner in board.Corners)
            {
                var area = FindCornerArea(corner.Slot);
                float centerCol = area.Col + (area.Size - 1) / 2f;
                float centerRow = area.Row + (area.Size - 1) / 2f;
                // 立着的建筑：脚站在角落地块中心，向上抬半个身位
                var worldPos = IsoProjection.WorldPosition(centerCol, centerRow) + new Vector3(0f, 0.9f, 0f);

                var instance = Instantiate(_cornerBuildingPrefab, worldPos, Quaternion.identity, transform);
                instance.name = $"Corner_{corner.Slot}_{corner.Building}";
                // 立起物统一排在所有地格之上，彼此间按纵深排
                instance.sortingOrder = 100 + (int)(centerCol + centerRow);

                var buildingSprite = _visuals.Buildings != null ? _visuals.Buildings.Resolve(corner.Building) : null;
                if (buildingSprite != null)
                {
                    instance.sprite = buildingSprite;
                    instance.color = Color.white;
                    instance.transform.localScale = Vector3.one;
                }
                else
                {
                    instance.transform.localScale = new Vector3(1.6f, 2.2f, 1f);
                }
            }

            // 落在地面位置，抬升与否由控制器按占位/真图自行决定
            _centerInstance = Instantiate(_centerPrefab,
                WorldPosition(BoardGeometry.BoardCenter),
                Quaternion.identity, transform);
            _centerInstance.name = "Center_Godzilla";
            _centerInstance.Initialize(board.Center, _visuals.CenterStates);
        }

        private static CornerArea FindCornerArea(CornerSlot slot)
        {
            foreach (var area in BoardGeometry.CornerAreas)
            {
                if (area.Slot == slot) return area;
            }
            return BoardGeometry.CornerAreas[0];
        }

        private void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
            _spawnedTiles.Clear();
        }
    }
}
