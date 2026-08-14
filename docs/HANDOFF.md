# 项目交接文档（2026-08-14）

> 本文档记录项目从零到当前状态的全部过程、决策与待办，供任何开发工具/助手接手时恢复上下文。
> 配套文档：`docs/art-spec.md`（素材规范）、`docs/art-prompts.md`（AI 出图提示词）、根目录 `README.md`。

## 一、项目概况

- **项目**：赛博东京飞行棋（Cyber Tokyo Flying Chess），学习向但按可上架产品标准做
- **引擎**：Unity 6.3 LTS (6000.3.22f1)，2D URP 模板，路径 `~/Desktop/CyberTokyoFlyingChess`
- **目标平台**：iOS + Android（目前只做了 iOS 模拟器，真机和 Android 未做）
- **开发者背景**：无游戏开发经验、无美术功底，正在学习；美术走占位 → AI 出图/自学画图替换的路线
- **后台**：`~/Desktop/feixingqi`（fly-game-admin），Cloudflare Worker + D1，**棋盘/奖励规则的唯一来源**，
  本项目的数据层是它的 C# 移植。本地跑法：`cd ~/Desktop/feixingqi && npm run dev`（localhost:8787，admin/admin）
- **另一客户端**：fly-game-app（Cocos，不在本机），与本项目并行，互不依赖

## 二、已完成（按提交顺序）

1. **仓库与目录骨架** —— git + LFS（美术二进制），`Assets/` 分层（Art/Audio/Prefabs/Data/Scenes/Scripts/Tests）
2. **数据层移植**（`Scripts/Core/`）—— `BoardGeometry`（十字 13×13、48 格环路、传送带路径，移植自后台 geometry.ts）、
   `BoardTypes`/`RewardTypes`（wire-string 兼容的枚举与 DTO）、`DefaultConfigFactory`（移植 defaults.ts 的**算法**而非抄数据）。
   **10 个 EditMode 测试全过**，含与后台 `/api/game/config` 快照的对账测试（fixture 在 `Tests/Fixtures/sample-config.json`）
3. **玩法闭环**（`Scripts/Gameplay/`）—— 棋盘渲染、掷骰、逐格移动（途中格触发 OnPass）、落地奖励、
   传送带进中心、哥斯拉状态机占位、回合轮转。Play 即可玩
4. **网络层**（`Scripts/Networking/`）—— 拉 `/api/game/config`，三级兜底：网络 → persistentDataPath 缓存 → 内置
   `DefaultGameConfig.asset`。状态栏显示配置来源和版本（`cfg: v1 (network)`）
5. **美术管线** —— `docs/art-spec.md` 规范 + `ArtImportPostprocessor` 自动导入设置 + 五张查找表 SO
   （palette/建筑/中心状态/奖励图标/棋子），全部"挂图用图、没图用占位色"，可逐张替换
6. **iOS 模拟器打包链** —— 全命令行：Unity batch → xcodebuild → simctl 装机，见下方"构建命令"
7. **等距视角** —— `IsoProjection`（2:1 菱形格），建筑/哥斯拉立起、画家排序，观感对齐设计稿的 2.5D
8. **视觉打磨首轮** —— URP Bloom/暗角/调色全局 Volume、霓虹配色、箭头格呼吸脉冲（`TilePulse`）、
   哥斯拉愤怒闪烁 + atomicBreath 粒子喷吐、棋子抛物线跳格

## 三、关键决策与坑（新工具必读）

1. **⚠️ 场景序列化坑（最重要）**：场景对 `CyberTokyo.Gameplay` 程序集类型的**资产**引用
   （prefab 上的自定义组件、该程序集的 SO）在存场景时会**静默置空**。场景内部引用、引擎内置类型
   资产引用、`CyberTokyo.Core` 程序集的 SO 引用不受影响。根因未挖到底。
   **对策**：这类资产全放 `Assets/Resources/`，运行时 `Resources.Load`（见 `GameLoopController.TryLoadResources`）。
   新增此类引用时照此办理，别改回 Inspector 拖拽。
2. **场景是生成物**：`Assets/Scenes/Game.unity` 由 `Phase3SceneBuilder.Build()`（菜单 Tools → Cyber Tokyo →
   Build Phase 3 Prefabs And Scene）全量生成，**手改场景会被下次重建覆盖**，要改就改 builder 代码。
   打包入口会自动先重建场景。
3. **模拟器架构**：ProjectSettings 的 `iOSSimulatorArchitecture: 1`（arm64）。Unity 默认 0（x86_64）
   在 Apple Silicon 模拟器上装不进去。改回 0 会复发。
4. **C# 静态字段初始化顺序**：`BoardGeometry` 里 `OutlineCorners` 必须声明在 `RingPath` 之前（文件内有注释），踩过 NPE
5. **数据层与后台的同步**：改后台 `geometry.ts`/`types.ts`/`defaults.ts` 后，重新 curl 一份
   `/api/game/config` 覆盖 `Tests/Fixtures/sample-config.json`，跑 EditMode 测试对账
6. Unity 打开项目时命令行不能再开第二个实例（Temp/UnityLockfile 锁）；批处理构建前要先退出编辑器

## 四、构建与运行命令

```bash
# 后台（先起它，客户端才拉得到配置）
cd ~/Desktop/feixingqi && npm run dev        # localhost:8787

# 一条龙出模拟器包并装机（需先退出 Unity 编辑器）
/Applications/Unity/Hub/Editor/6000.3.22f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -quit -projectPath ~/Desktop/CyberTokyoFlyingChess \
  -buildTarget iOS -executeMethod CyberTokyo.Editor.BuildSetupTool.BuildIOSSimulator \
  -logFile /tmp/unity_build.log
cd ~/Desktop/CyberTokyoFlyingChess/Builds/iOS
xcodebuild -project Unity-iPhone.xcodeproj -scheme Unity-iPhone -configuration Release \
  -destination 'generic/platform=iOS Simulator' -derivedDataPath ./DerivedData build
xcrun simctl install booted DerivedData/Build/Products/Release-iphonesimulator/CyberTokyoFlyingChess.app
xcrun simctl launch booted com.sunandsong.cybertokyoflyingchess
```

编辑器内测试：Window → General → Test Runner → EditMode → Run All（10 个应全绿）。

## 五、待办清单

### 近期（按优先级）

- [ ] **出第一批 AI 图并挂进游戏**（清单见下节，提示词在 `docs/art-prompts.md`）
- [ ] iOS 真机：Tools → Cyber Tokyo → iOS Target - Device 切回真机 SDK，Xcode 配免费 Apple ID 签名
- [ ] Android 包：模块已装好，需要写 BuildAndroid 方法（AAB + Play App Signing），Google Play 账号 $25 未注册
- [ ] 后台部署上线（wrangler.jsonc 的 database_id 还是占位符）——真机测试必须有公网 https 地址，
  `Assets/Resources/Data/GameServerSettings.asset` 的 BaseUrl 要跟着换

### 玩法未决项（继承自后台，代码里都有 TODO OPEN-n 标注，占位规则就地可换）

- [ ] OPEN-1 哪些格带奖励（现为客户端开局随机，`RewardPlacement.cs`，标了 TEMP 该删）
- [ ] OPEN-2 哥斯拉状态切换条件（现为每次有棋子抵达中心就轮换下一状态）
- [ ] OPEN-3 棋子到中心后去哪（现为下回合从箭头格下一格重进环路）
- [ ] OPEN-4 自由传送格规则（现为无效果）
- [ ] OPEN-5 四角建筑踩上效果（现为无效果）
- [ ] OPEN-6 胜利条件（现为无限自由玩）

### 远期

- [ ] 幽灵对手（后台核心待做功能，客户端要存版本号回放轨迹——版本号已在下发协议里）
- [ ] 中心状态素材走后台 `/api/game/asset` 远程加载（本地查找表做兜底，结构已预留）
- [ ] 音效/音乐（`Assets/Audio/` 空着）
- [ ] 骰子动画、UI 美化（现在是最朴素的一个按钮一行字）

## 六、素材清单与来源（重要更新）

**设计师的正式素材已经存在**，大部分不需要 AI 出图：

- `docs/design/0731.../图片和附件/sucai.png`（8844×3144，带透明通道）是**素材总表**：
  四只动物飞行器棋子、四栋建筑（牌坊/东京塔/浅草寺/招财猫神社）、等距地格、奖励图标全在里面
- `图层 165 拷贝 3 / 166 / 167 / 168.png` 是哥斯拉四状态的独立透明图 —— **已接入完成**
  （改名放进 `Art/Sprites/Center/`，Auto-Wire 自动挂表）

**接入方法（已验证跑通）**：素材按 `docs/art-spec.md` 的命名放进 `Assets/Art/Sprites/<类别>/`，
跑菜单 Tools → Cyber Tokyo → Auto-Wire Art From Sprites（或直接出包，会自动跑），按文件名自动挂进查找表。

| 状态 | 素材 | 文件名 | 放置目录 |
|---|---|---|---|
| ✅ 已接入 | 哥斯拉四状态 | `center_sleeping/angry/atomicBreath/pleased.png` | `Center/` |
| ⬜ 待从 sucai.png 切 | 棋子 ×4 | `piece_green/yellow/red/blue.png` | `Pieces/` |
| ⬜ 待从 sucai.png 切 | 建筑 ×4 | `building_startGate/tokyoTower/sensojiPagoda/luckyCatShrine.png` | `Buildings/` |
| ⬜ 待从 sucai.png 切 | 地格 ×4 | `tile_green/yellow/red/blue.png` | `Board/` |
| ⬜ 待从 sucai.png 切 | 奖励图标 ×5 | `reward_coin/banknote/dice/card_shard/mystery.png` | `Reward/` |

切图注意：sucai.png 的元素排布是不规则的，需要按坐标裁切（macOS 可用 `sips -c` 或 Photoshop/Python PIL）。
棋子贴纸在表右上区、建筑在最右侧、地格在左上/左中、图标在左中偏下。切出后确认透明背景、
裁到元素贴边。AI 出图提示词（`docs/art-prompts.md`）仅在需要补设计师没画的素材时才用。
