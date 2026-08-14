using System.Collections;
using System.Collections.Generic;
using CyberTokyo.Core;
using CyberTokyo.Core.Board;
using CyberTokyo.Core.Reward;
using CyberTokyo.Core.State;
using CyberTokyo.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace CyberTokyo.Gameplay
{
    /// <summary>
    /// 玩法闭环的胶水层：摇骰子 -&gt; 验证轮到谁 -&gt; 逐格挪动（中途格触发 OnPass）
    /// -&gt; 落地效果（OnLand / 传送带 / 未决项占位）-&gt; 换下一个玩家。
    ///
    /// Phase 3 阶段数据来自 Inspector 拖进来的 DefaultGameConfigAsset；Phase 4
    /// 接入网络后，这里只需要把 Start() 里取配置那一行换成 ConfigRepository.LoadAsync()，
    /// 其余流程不用动。
    /// </summary>
    public class GameLoopController : MonoBehaviour
    {
        [SerializeField] private DefaultGameConfigAsset offlineConfig;
        [SerializeField] private BoardRenderer boardRenderer;
        [SerializeField] private DiceController diceController;
        [SerializeField] private Button rollButton;
        [SerializeField] private Text statusText;

        [SerializeField] private float stepDuration = 0.18f;

        // 见 BoardRenderer 里的注释：场景对 Gameplay 程序集资产的引用存不住，
        // 这类东西统一走 Resources 运行时加载
        private PieceController _piecePrefab;

        private GameState _gameState;
        private TurnManager _turnManager;
        private RewardApplier _rewardApplier;
        private readonly Dictionary<TileColor, PieceController> _pieceViews = new Dictionary<TileColor, PieceController>();
        private bool _isMoving;
        private LoadedConfig _loadedConfig;

        private IEnumerator Start()
        {
            if (!TryLoadResources()) yield break;

            // 三级兜底拉配置：网络 → 缓存 → 内置。settings 资产不存在就直接离线模式
            var settings = Resources.Load<GameServerSettings>("Data/GameServerSettings");
            yield return ConfigRepository.Load(settings, offlineConfig, loaded => _loadedConfig = loaded);

            Debug.Log($"[GameLoop] config source={_loadedConfig.Source}, " +
                      $"board v{_loadedConfig.BoardVersion}, reward v{_loadedConfig.RewardVersion}");

            var board = CloneBoard(_loadedConfig.Board);
            var reward = _loadedConfig.Reward;

            _gameState = new GameState(board, reward);
            RewardPlacement.ApplyTemporaryPlacement(_gameState.Board, _gameState.Reward, _gameState.Level);

            boardRenderer.Render(_gameState.Board);
            boardRenderer.RefreshRewardMarkers();
            _turnManager = new TurnManager(_gameState);
            _rewardApplier = new RewardApplier(new RewardLedger());

            SpawnPieces();

            if (rollButton != null) rollButton.onClick.AddListener(OnRollButtonClicked);
            UpdateStatusText();
        }

        private BoardVisuals _visuals;

        private bool TryLoadResources()
        {
            var tileGo = Resources.Load<GameObject>("Board/TileView");
            var cornerGo = Resources.Load<GameObject>("Board/CornerBuildingView");
            var centerGo = Resources.Load<GameObject>("Board/CenterView");
            var pieceGo = Resources.Load<GameObject>("Pieces/PieceView");
            _visuals = BoardVisuals.LoadFromResources();

            if (tileGo == null || cornerGo == null || centerGo == null || pieceGo == null || _visuals.Palette == null)
            {
                Debug.LogError($"[GameLoop] Resources 加载失败: tile={tileGo != null}, corner={cornerGo != null}, " +
                               $"center={centerGo != null}, piece={pieceGo != null}, palette={_visuals.Palette != null}。" +
                               "确认这些资产都在 Assets/Resources/ 对应子目录下");
                return false;
            }

            _piecePrefab = pieceGo.GetComponent<PieceController>();
            boardRenderer.Initialize(
                tileGo.GetComponent<TileView>(),
                cornerGo.GetComponent<SpriteRenderer>(),
                centerGo.GetComponent<CenterGodzillaController>(),
                _visuals);
            return true;
        }

        private void SpawnPieces()
        {
            for (int i = 0; i < _gameState.Players.Count; i++)
            {
                var player = _gameState.Players[i];
                var piece = Instantiate(_piecePrefab,
                    boardRenderer.WorldPositionForRingIndex(player.RingIndex) + PieceOffset(i),
                    Quaternion.identity);
                piece.name = $"Piece_{player.Color}";
                piece.SetColor(PlaceholderColorFor(player.Color));
                var pieceSprite = _visuals.Pieces != null ? _visuals.Pieces.Resolve(player.Color) : null;
                if (pieceSprite != null) piece.SetSprite(pieceSprite);
                _pieceViews[player.Color] = piece;
            }
        }

        /// <summary>四个棋子按象限微错位，叠在同一格时也能看清都有谁。
        /// 等距菱形格半高只有 0.25，偏移量要比正方格时代收紧</summary>
        private static Vector3 PieceOffset(int playerIndex)
        {
            const float d = 0.11f;
            switch (playerIndex % 4)
            {
                case 0: return new Vector3(-d, d, 0);
                case 1: return new Vector3(d, d, 0);
                case 2: return new Vector3(-d, -d, 0);
                default: return new Vector3(d, -d, 0);
            }
        }

        private void OnRollButtonClicked()
        {
            if (_isMoving) return;
            StartCoroutine(RollAndMoveRoutine());
        }

        private IEnumerator RollAndMoveRoutine()
        {
            _isMoving = true;

            var player = _turnManager.CurrentPlayer;
            int roll = diceController.Roll();
            var piece = _pieceViews[player.Color];
            var offset = PieceOffset(_gameState.Players.IndexOf(player));

            var waypoints = new List<Vector3>(roll);
            int startIndex = player.RingIndex;
            for (int step = 1; step <= roll; step++)
            {
                waypoints.Add(boardRenderer.WorldPositionForRingIndex(startIndex + step) + offset);
            }

            int landedIndex = startIndex;
            yield return StartCoroutine(piece.StepAlong(waypoints, stepDuration, stepNumber =>
            {
                landedIndex = (startIndex + stepNumber + 1) % BoardGeometry.RingTileCount;
                var tile = _gameState.Board.Tiles[landedIndex];
                bool isFinalStep = stepNumber == waypoints.Count - 1;

                if (!isFinalStep)
                {
                    _rewardApplier.ApplyOnPass(player, tile);
                }
            }));

            player.RingIndex = landedIndex;
            var landedTile = _gameState.Board.Tiles[landedIndex];
            _rewardApplier.ApplyOnPass(player, landedTile);

            if (landedTile.Kind == TileKind.ConveyorTrigger)
            {
                yield return StartCoroutine(RunConveyorRoutine(player, piece, landedTile));
            }
            else
            {
                _rewardApplier.ApplyOnLand(player, landedTile);
                // TODO OPEN-4: free_teleport 目的地规则未定，落到这种格子先什么都不做。
                // TODO OPEN-5: corner_building 踩上去的效果未定，同上先什么都不做。
            }

            Debug.Log($"[GameLoop] {player.Color} rolled {roll}, landed on tile {player.RingIndex} ({landedTile.Kind})");

            _turnManager.NextTurn();
            UpdateStatusText();
            _isMoving = false;
        }

        private IEnumerator RunConveyorRoutine(PlayerState player, PieceController piece, TileConfigDto triggerTile)
        {
            var conveyor = ConveyorMover.FindConveyorAt(_gameState.Board, triggerTile.Index);
            if (conveyor == null) yield break;

            var path = ConveyorMover.GetPathToCenter(conveyor);
            var waypoints = new List<Vector3>(path.Count);
            foreach (var pos in path) waypoints.Add(BoardRenderer.WorldPosition(pos));

            yield return StartCoroutine(piece.StepAlong(waypoints, stepDuration, null));

            _rewardApplier.ApplyConveyorEnd(player, _gameState.Reward.ConveyorEndReward);
            boardRenderer.CenterInstance.OnPieceReachedCenter();

            int reentryIndex = ConveyorMover.ResolveReentryRingIndex(conveyor);
            piece.SnapTo(boardRenderer.WorldPositionForRingIndex(reentryIndex) + PieceOffset(_gameState.Players.IndexOf(player)));
            player.RingIndex = reentryIndex;
        }

        private void UpdateStatusText()
        {
            if (statusText == null) return;
            var player = _turnManager.CurrentPlayer;
            string cfg = _loadedConfig.Source == "builtin"
                ? "cfg: builtin"
                : $"cfg: v{_loadedConfig.BoardVersion} ({_loadedConfig.Source})";
            statusText.text = $"Turn: {player.Color}  |  Level: {_gameState.Level}  |  {cfg}";
        }

        private static Color PlaceholderColorFor(TileColor color)
        {
            switch (color)
            {
                case TileColor.Green: return new Color(0.25f, 0.75f, 0.35f);
                case TileColor.Yellow: return new Color(0.95f, 0.85f, 0.20f);
                case TileColor.Red: return new Color(0.85f, 0.25f, 0.25f);
                case TileColor.Blue: return new Color(0.25f, 0.45f, 0.90f);
                default: return Color.white;
            }
        }

        /// <summary>
        /// RewardPlacement 会在 tiles 上就地写 OnLand/OnPass —— offlineConfig 是共享的
        /// ScriptableObject 资产，不深拷贝的话每次 Play 都会在同一份资产上累加写入。
        /// </summary>
        private static BoardConfigDto CloneBoard(BoardConfigDto source)
        {
            var clone = new BoardConfigDto
            {
                Form = source.Form,
                Corners = source.Corners,
                Conveyors = source.Conveyors,
                Center = source.Center,
                Tiles = new List<TileConfigDto>(source.Tiles.Count),
            };

            foreach (var tile in source.Tiles)
            {
                clone.Tiles.Add(new TileConfigDto
                {
                    Index = tile.Index,
                    Kind = tile.Kind,
                    Colors = new List<TileColor>(tile.Colors),
                    OnPass = null,
                    OnLand = null,
                });
            }

            return clone;
        }
    }
}
