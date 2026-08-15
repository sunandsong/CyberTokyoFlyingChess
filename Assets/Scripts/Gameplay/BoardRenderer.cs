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
            RenderBoardEdgeBuildings();

            foreach (var tile in board.Tiles)
            {
                var pos = BoardGeometry.RingPosition(tile.Index);
                var instance = Instantiate(_tilePrefab, WorldPosition(pos), Quaternion.identity, transform);
                instance.name = $"Tile_{tile.Index:D2}_{tile.Kind}";
                // 占位菱形贴图是 2:1 的，纵向拉伸到当前投影角度
                instance.transform.localScale = new Vector3(1f, IsoProjection.TileArtStretchY, 1f);
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
                    // 落在 3x3 区域的前角位置。贴图原始宽度未必正好等于 3 格宽，
                    // 按底座压满 3x3 区域反推缩放，底座边才能跟格子边平行对齐
                    instance.sprite = buildingSprite;
                    instance.color = Color.white;
                    float targetWidth = area.Size * IsoProjection.TileWidth;
                    float fit = targetWidth / buildingSprite.bounds.size.x;
                    instance.transform.localScale = new Vector3(fit, fit, 1f);
                    float frontDrop = area.Size * IsoProjection.TileHeight / 2f;
                    instance.transform.position = ground + new Vector3(0f, -frontDrop, 0f);
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
        // 覆盖"镜头跟随移动时可能看到的最大范围"：竖屏视野半高 ~8.5 + 跟随
        // 纵向偏移，加余量取 iso y ∈ [-11, 11]、x ∈ [-8, 8]，按等距坐标反推
        // 网格范围再逐格过滤，别整片方阵全生成（会多出几千个没人看见的格子）
        private const float VisibleMaxX = 8f;
        private const float VisibleMaxY = 11f;

        private void RenderFloor()
        {
            var sprite = _tilePrefab.GetComponent<SpriteRenderer>().sprite;
            var dark = new Color(0.085f, 0.085f, 0.135f);
            var darker = new Color(0.068f, 0.068f, 0.112f);
            var grass = new Color(0.10f, 0.30f, 0.15f);

            var root = new GameObject("Floor");
            root.transform.SetParent(transform, false);

            for (int col = -34; col <= BoardGeometry.RingSide + 33; col++)
            {
                for (int row = -34; row <= BoardGeometry.RingSide + 33; row++)
                {
                    var pos = IsoProjection.WorldPosition(col, row);
                    if (Mathf.Abs(pos.x) > VisibleMaxX || Mathf.Abs(pos.y) > VisibleMaxY) continue;

                    var go = new GameObject($"F_{col}_{row}");
                    go.transform.SetParent(root.transform, false);
                    go.transform.position = pos;
                    go.transform.localScale = new Vector3(1f, IsoProjection.TileArtStretchY, 1f);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = sprite;
                    // 没图之前的占位草地：按坐标哈希撒一些绿地块，别是纯地砖一片
                    int hash = ((col * 928371 + row * 123457) & 0x7fffffff) % 11;
                    sr.color = hash < 2 ? grass : (((col + row) & 1) == 0 ? dark : darker);
                    sr.sortingOrder = -50;
                }
            }
        }

        /// <summary>背景霓虹楼群：以网格铺满镜头可能看到的整个范围（棋盘本体所在的
        /// 13x13 方框除外），Resources/Decor 里有多少张就轮着摆，填到屏幕边缘不留空。
        /// 没有素材时静默跳过。</summary>
        private void RenderDecor()
        {
            var sprites = Resources.LoadAll<Sprite>("Decor");
            if (sprites == null || sprites.Length == 0) return;

            var root = new GameObject("Decor");
            root.transform.SetParent(transform, false);

            // 前后遮挡关系已经靠深度排序保证正确，楼本身不用再缩小来避重叠
            const int step = 2;
            const float buildingScale = 1f;
            int index = 0;
            for (int col = -34; col <= BoardGeometry.RingSide + 33; col += step)
            {
                for (int row = -34; row <= BoardGeometry.RingSide + 33; row += step)
                {
                    // 棋盘本体（含四角建筑）落在这个方框里。col/row 越小越靠屏幕上方、
                    // 越大越靠屏幕下方（见 IsoProjection 里 y 的算法）。margin 只能往外扩
                    // 留缓冲，不能缩到棋盘自己的 0..RingSide-1 范围以内——否则楼群网格会
                    // 直接在棋盘内部（比如某一整列正好是行进臂）生成，从里面"顶"出来
                    const int topMargin = -2;
                    const int otherMargin = 2;
                    int lowerBound = -Mathf.Max(topMargin, 0);
                    int upperBound = BoardGeometry.RingSide - 1 + Mathf.Max(otherMargin, 0);
                    if (col >= lowerBound && col <= upperBound
                        && row >= lowerBound && row <= upperBound) continue;

                    var pos = IsoProjection.WorldPosition(col, row);
                    if (Mathf.Abs(pos.x) > VisibleMaxX || Mathf.Abs(pos.y) > VisibleMaxY) continue;

                    var go = new GameObject($"Decor_{col}_{row}");
                    go.transform.SetParent(root.transform, false);
                    go.transform.position = pos;
                    go.transform.localScale = Vector3.one * buildingScale;
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = sprites[index % sprites.Length];
                    // 画家算法：楼和地格用同一套深度标准（col+row，地格见
                    // IsoProjection.SortOrder）。楼在棋盘后面（sum 小）被棋盘盖住，
                    // 在棋盘前面（屏幕下方，sum 大）就该反过来盖住棋盘——近的挡远的
                    sr.sortingOrder = col + row;
                    index++;
                }
            }
        }

        /// <summary>沿十字轮廓给每个环路格的"朝外邻格"精确落一栋楼——坐标严格落在
        /// 等距网格上，楼群像一圈城墙一样严丝合缝贴着棋盘。排序与地格同一套画家算法：
        /// 棋盘上缘的楼被路盖住、下缘的楼盖住路，近的挡远的</summary>
        private void RenderBoardEdgeBuildings()
        {
            var sprites = Resources.LoadAll<Sprite>("Decor");
            if (sprites == null || sprites.Length == 0) return;

            var root = new GameObject("EdgeBuildings");
            root.transform.SetParent(transform, false);

            var placed = new HashSet<(int, int)>();
            var directions = new[] { (1, 0), (-1, 0), (0, 1), (0, -1) };
            int index = 0;

            for (int i = 0; i < BoardGeometry.RingTileCount; i++)
            {
                var pos = BoardGeometry.RingPosition(i);
                foreach (var (dc, dr) in directions)
                {
                    // 只贴屏幕上方那一侧（col+row 变小 = iso 投影里往上）——
                    // 棋盘下缘的楼维持原来的摆法，不动
                    if (dc + dr >= 0) continue;

                    int nc = pos.Col + dc;
                    int nr = pos.Row + dr;
                    // 只要严格在 13x13 之外的邻格——棋盘框内的空位是四角建筑区，不占
                    bool outside = nc < 0 || nc > BoardGeometry.RingSide - 1
                                || nr < 0 || nr > BoardGeometry.RingSide - 1;
                    if (!outside || !placed.Add((nc, nr))) continue;

                    var go = new GameObject($"Edge_{nc}_{nr}");
                    go.transform.SetParent(root.transform, false);
                    go.transform.position = IsoProjection.WorldPosition(nc, nr);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = sprites[index % sprites.Length];
                    sr.sortingOrder = nc + nr;
                    index++;
                }
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
