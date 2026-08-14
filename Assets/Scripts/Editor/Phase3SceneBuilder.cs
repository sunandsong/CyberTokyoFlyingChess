using System.IO;
using CyberTokyo.Core;
using CyberTokyo.Core.Board;
using CyberTokyo.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CyberTokyo.Editor
{
    /// <summary>
    /// 一键搭出 Phase 3 的占位美术、prefab 与 Assets/Scenes/Game.unity。
    /// 幂等 —— 已存在的资产不会重建，只有场景本身每次全新生成（场景是装配结果，
    /// 手改场景里的摆法不如手改这份脚本再重跑）。
    ///
    /// ⚠️ prefab 和调色板放在 Assets/Resources/ 下、由 GameLoopController 运行时
    /// Resources.Load，而不是绑在场景引用上 —— 场景对 Gameplay 程序集类型的资产引用
    /// 存场景时会被静默置空（Core 程序集的 SO 和场景内部引用不受影响，实测踩过）。
    /// 场景里只绑证明存得住的那几类。
    ///
    /// 占位美术就是白色方块/圆形贴图，靠 SpriteRenderer.color 上色 —— Phase 5
    /// 真美术接进来后，这几个 Sprite 字段换成真图，脚本这边不用动。
    /// </summary>
    public static class Phase3SceneBuilder
    {
        private const string SquareSpritePath = "Assets/Art/Sprites/UI/placeholder_square.png";
        private const string CircleSpritePath = "Assets/Art/Sprites/UI/placeholder_circle.png";
        private const string DiamondSpritePath = "Assets/Art/Sprites/UI/placeholder_diamond.png";
        private const string PalettePath = "Assets/Resources/Data/TileColorPalette.asset";
        private const string TilePrefabPath = "Assets/Resources/Board/TileView.prefab";
        private const string CornerPrefabPath = "Assets/Resources/Board/CornerBuildingView.prefab";
        private const string CenterPrefabPath = "Assets/Resources/Board/CenterView.prefab";
        private const string PiecePrefabPath = "Assets/Resources/Pieces/PieceView.prefab";
        private const string DefaultConfigPath = "Assets/Data/DefaultGameConfig.asset";
        private const string ScenePath = "Assets/Scenes/Game.unity";

        [MenuItem("Tools/Cyber Tokyo/Build Phase 3 Prefabs And Scene")]
        public static void Build()
        {
            var squareSprite = EnsureSprite(SquareSpritePath, MakeSquareTexture());
            var circleSprite = EnsureSprite(CircleSpritePath, MakeCircleTexture());
            var diamondSprite = EnsureSprite(DiamondSpritePath, MakeDiamondTexture());
            EnsurePalette();

            EnsureTilePrefab(diamondSprite);
            EnsureCornerPrefab(squareSprite);
            EnsureCenterPrefab(squareSprite);
            EnsurePiecePrefab(circleSprite);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            BuildScene();
            VerifySceneBindings();

            Debug.Log("[Phase3SceneBuilder] Built prefabs and Assets/Scenes/Game.unity");
        }

        // ── 占位贴图 ─────────────────────────────────────────────

        private static Texture2D MakeSquareTexture()
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>等距地格的 2:1 菱形（128x64），白色，靠 SpriteRenderer.color 上色</summary>
        private static Texture2D MakeDiamondTexture()
        {
            const int w = 128;
            const int h = 64;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // |x/半宽| + |y/半高| <= 1 即菱形内
                    float nx = Mathf.Abs(x + 0.5f - w / 2f) / (w / 2f);
                    float ny = Mathf.Abs(y + 0.5f - h / 2f) / (h / 2f);
                    byte alpha = nx + ny <= 1f ? (byte)255 : (byte)0;
                    pixels[y * w + x] = new Color32(255, 255, 255, alpha);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        private static Texture2D MakeCircleTexture()
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            float radius = size / 2f - 2f;
            var center = new Vector2(size / 2f, size / 2f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    byte alpha = dist <= radius ? (byte)255 : (byte)0;
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        private static Sprite EnsureSprite(string path, Texture2D fallbackTexture)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null)
            {
                Object.DestroyImmediate(fallbackTexture);
                return existing;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, fallbackTexture.EncodeToPNG());
            Object.DestroyImmediate(fallbackTexture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 128;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ── 占位配色 ─────────────────────────────────────────────

        private static TileColorPaletteSO EnsurePalette()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TileColorPaletteSO>(PalettePath);
            if (existing != null) return existing;

            var palette = ScriptableObject.CreateInstance<TileColorPaletteSO>();
            palette.Entries = new[]
            {
                new TileColorPaletteSO.Entry { Color = TileColor.Green, DisplayColor = new Color(0.25f, 0.75f, 0.35f) },
                new TileColorPaletteSO.Entry { Color = TileColor.Yellow, DisplayColor = new Color(0.95f, 0.85f, 0.20f) },
                new TileColorPaletteSO.Entry { Color = TileColor.Red, DisplayColor = new Color(0.85f, 0.25f, 0.25f) },
                new TileColorPaletteSO.Entry { Color = TileColor.Blue, DisplayColor = new Color(0.25f, 0.45f, 0.90f) },
            };

            AssetDatabase.CreateAsset(palette, PalettePath);
            return palette;
        }

        // ── prefab ─────────────────────────────────────────────

        private static TileView EnsureTilePrefab(Sprite sprite)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(TilePrefabPath);
            if (existing != null) return existing.GetComponent<TileView>();

            var go = new GameObject("TileView");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 0;
            go.transform.localScale = Vector3.one * 0.92f;

            var view = go.AddComponent<TileView>();
            BindPrivateField(view, "spriteRenderer", sr);

            return SaveAndDestroy(go, TilePrefabPath).GetComponent<TileView>();
        }

        private static SpriteRenderer EnsureCornerPrefab(Sprite sprite)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(CornerPrefabPath);
            if (existing != null) return existing.GetComponent<SpriteRenderer>();

            var go = new GameObject("CornerBuildingView");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.40f, 0.40f, 0.46f);
            sr.sortingOrder = -1;

            return SaveAndDestroy(go, CornerPrefabPath).GetComponent<SpriteRenderer>();
        }

        private static CenterGodzillaController EnsureCenterPrefab(Sprite sprite)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(CenterPrefabPath);
            if (existing != null) return existing.GetComponent<CenterGodzillaController>();

            var go = new GameObject("CenterView");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            // 立起物层：100 + 纵深(中心 6+6=12)
            sr.sortingOrder = 112;
            go.transform.localScale = new Vector3(1.5f, 2f, 1f);

            var controller = go.AddComponent<CenterGodzillaController>();
            BindPrivateField(controller, "spriteRenderer", sr);

            return SaveAndDestroy(go, CenterPrefabPath).GetComponent<CenterGodzillaController>();
        }

        private static PieceController EnsurePiecePrefab(Sprite sprite)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PiecePrefabPath);
            if (existing != null) return existing.GetComponent<PieceController>();

            // 白色底圈 + 内圈填充色：占位棋子和占位格子共用一套纯色，
            // 没这圈描边的话棋子落到同色格上就隐身了
            var go = new GameObject("PieceView");
            var outline = go.AddComponent<SpriteRenderer>();
            outline.sprite = sprite;
            outline.color = Color.white;
            // 棋子永远压在地格和立起物之上（体积小，偶尔穿插可接受）
            outline.sortingOrder = 200;
            go.transform.localScale = Vector3.one * 0.6f;

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(go.transform, false);
            fillGo.transform.localScale = Vector3.one * 0.72f;
            var fill = fillGo.AddComponent<SpriteRenderer>();
            fill.sprite = sprite;
            fill.sortingOrder = 201;

            var controller = go.AddComponent<PieceController>();
            BindPrivateField(controller, "spriteRenderer", fill);

            return SaveAndDestroy(go, PiecePrefabPath).GetComponent<PieceController>();
        }

        private static GameObject SaveAndDestroy(GameObject go, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static void BindPrivateField(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();

            // objectReferenceValue 类型不匹配时会静默置 null，这里立刻读回来对账，
            // 把静默失败变成看得见的报错
            var readBack = new SerializedObject(target).FindProperty(fieldName).objectReferenceValue;
            if (value != null && readBack == null)
            {
                Debug.LogError($"[Phase3SceneBuilder] 绑定丢失: {target.name}.{fieldName} <- {value.name} " +
                               $"(赋进去读回来是 null，多半是序列化层认为类型不匹配)");
            }
        }

        /// <summary>存完场景后把场景里保留的绑定读回来核对一遍，缺了哪个直接报错指名道姓。
        /// Resources.Load 的那部分在 GameLoopController.TryLoadResources 里有自己的运行时检查。</summary>
        private static void VerifySceneBindings()
        {
            var gameLoop = Object.FindFirstObjectByType<GameLoopController>();
            if (gameLoop == null)
            {
                Debug.LogError("[Phase3SceneBuilder] verify: 场景里找不到 GameLoopController");
                return;
            }

            var so = new SerializedObject(gameLoop);
            string[] fields = { "offlineConfig", "boardRenderer", "diceController", "rollButton", "statusText" };
            foreach (var f in fields)
            {
                var v = so.FindProperty(f).objectReferenceValue;
                if (v == null) Debug.LogError($"[Phase3SceneBuilder] verify: GameLoop.{f} 是 None");
                else Debug.Log($"[Phase3SceneBuilder] verify: GameLoop.{f} -> {v.name} OK");
            }
        }

        // ── 场景 ─────────────────────────────────────────────

        private static void BuildScene()
        {
            EditorSceneManager.SaveOpenScenes();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var light = GameObject.Find("Directional Light");
            if (light != null) Object.DestroyImmediate(light);

            var cameraGo = GameObject.Find("Main Camera");
            var camera = cameraGo.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 8f; // 初值而已，CameraFitter 每帧按宽高比修正
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.backgroundColor = new Color(0.08f, 0.08f, 0.10f);
            cameraGo.AddComponent<CameraFitter>();

            // BoardRenderer 的 prefab/palette 依赖不在这里绑 —— 运行时 Resources.Load，
            // 原因见类头注释
            var boardRoot = new GameObject("BoardRoot");
            var boardRenderer = boardRoot.AddComponent<BoardRenderer>();

            var diceGo = new GameObject("DiceController");
            var dice = diceGo.AddComponent<DiceController>();

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var rollButton = CreateButton(canvasGo.transform, "RollButton", "Roll Dice", font, new Vector2(0.5f, 0.08f));
            var statusText = CreateText(canvasGo.transform, "StatusText", "Turn: -", font, new Vector2(0.5f, 0.92f));

            var offlineConfig = AssetDatabase.LoadAssetAtPath<DefaultGameConfigAsset>(DefaultConfigPath);

            var gameLoopGo = new GameObject("GameLoop");
            var gameLoop = gameLoopGo.AddComponent<GameLoopController>();
            BindPrivateField(gameLoop, "offlineConfig", offlineConfig);
            BindPrivateField(gameLoop, "boardRenderer", boardRenderer);
            BindPrivateField(gameLoop, "diceController", dice);
            BindPrivateField(gameLoop, "rollButton", rollButton);
            BindPrivateField(gameLoop, "statusText", statusText);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static Button CreateButton(Transform parent, string name, string label, Font font, Vector2 anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(280, 100);
            rect.anchoredPosition = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.color = new Color(0.20f, 0.55f, 0.95f);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var text = textGo.GetComponent<Text>();
            text.text = label;
            text.font = font;
            text.fontSize = 42;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            return go.GetComponent<Button>();
        }

        private static Text CreateText(Transform parent, string name, string content, Font font, Vector2 anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(800, 100);
            rect.anchoredPosition = Vector2.zero;

            var text = go.GetComponent<Text>();
            text.text = content;
            text.font = font;
            text.fontSize = 48;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            return text;
        }
    }
}
