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
            RenderFloor();
            RenderDecor();

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
                var ground = IsoProjection.WorldPosition(centerCol, centerRow);

                var instance = Instantiate(_cornerBuildingPrefab, ground, Quaternion.identity, transform);
                instance.name = $"Corner_{corner.Slot}_{corner.Building}";
                // 立起物统一排在所有地格之上，彼此间按纵深排
                instance.sortingOrder = 100 + (int)(centerCol + centerRow);

                var buildingSprite = _visuals.Buildings != null ? _visuals.Buildings.Resolve(corner.Building) : null;
                if (buildingSprite != null)
                {
                    // 真图自带等距底座、底边中点锚点：图的底边就是底座最前角，
                    // 落在 3x3 区域的前角位置
                    instance.sprite = buildingSprite;
                    instance.color = Color.white;
                    instance.transform.localScale = Vector3.one;
                    instance.transform.position = ground + new Vector3(0f, -0.75f, 0f);
                }
                else
                {
                    // 占位灰块是居中锚点，抬半个身位站在区域中心
                    instance.transform.localScale = new Vector3(1.6f, 2.2f, 1f);
                    instance.transform.position = ground + new Vector3(0f, 0.9f, 0f);
                }
            }

            // 落在地面位置，抬升与否由控制器按占位/真图自行决定
            _centerInstance = Instantiate(_centerPrefab,
                WorldPosition(BoardGeometry.BoardCenter),
                Quaternion.identity, transform);
            _centerInstance.name = "Center_Godzilla";
            _centerInstance.Initialize(board.Center, _visuals.CenterStates);
        }

        /// <summary>
        /// 棋盘四周铺一圈暗色地砖网格：竖屏手机上棋盘只占屏幕中段，纯黑背景显得
        /// 又空又小，铺上夜色街区地面之后棋盘是"城市里的一块场地"而不是悬在虚空里。
        /// </summary>
        private void RenderFloor()
        {
            var sprite = _tilePrefab.GetComponent<SpriteRenderer>().sprite;
            var dark = new Color(0.085f, 0.085f, 0.135f);
            var darker = new Color(0.068f, 0.068f, 0.112f);

            var root = new GameObject("Floor");
            root.transform.SetParent(transform, false);

            // 覆盖"镜头跟随移动时可能看到的最大范围"：竖屏视野半高 ~8.5 + 跟随
            // 纵向偏移，加余量取 iso y ∈ [-11, 11]、x ∈ [-8, 8]，按等距坐标反推
            // 网格范围再逐格过滤，别整片方阵全生成（会多出几千个没人看见的格子）
            const float maxX = 8f;
            const float maxY = 11f;
            for (int col = -34; col <= BoardGeometry.RingSide + 33; col++)
            {
                for (int row = -34; row <= BoardGeometry.RingSide + 33; row++)
                {
                    var pos = IsoProjection.WorldPosition(col, row);
                    if (Mathf.Abs(pos.x) > maxX || Mathf.Abs(pos.y) > maxY) continue;

                    var go = new GameObject($"F_{col}_{row}");
                    go.transform.SetParent(root.transform, false);
                    go.transform.position = pos;
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = sprite;
                    sr.color = ((col + row) & 1) == 0 ? dark : darker;
                    sr.sortingOrder = -50;
                }
            }
        }

        /// <summary>装饰楼群的落点（方格坐标），环绕棋盘、避开四角建筑和行进臂</summary>
        private static readonly Vector2[] DecorSpots =
        {
            new Vector2(2, -3), new Vector2(8, -4), new Vector2(-4, 4), new Vector2(16, 3),
            new Vector2(-3, 11), new Vector2(5, 17), new Vector2(12, 17), new Vector2(17, 9),
        };

        /// <summary>背景霓虹楼群：Resources/Decor 里有多少张就轮着摆。没有素材时静默跳过</summary>
        private void RenderDecor()
        {
            var sprites = Resources.LoadAll<Sprite>("Decor");
            if (sprites == null || sprites.Length == 0) return;

            var root = new GameObject("Decor");
            root.transform.SetParent(transform, false);

            for (int i = 0; i < DecorSpots.Length; i++)
            {
                var spot = DecorSpots[i];
                var go = new GameObject($"Decor_{i}");
                go.transform.SetParent(root.transform, false);
                go.transform.position = IsoProjection.WorldPosition(spot.x, spot.y);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprites[i % sprites.Length];
                sr.sortingOrder = 100 + (int)(spot.x + spot.y);
            }
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
