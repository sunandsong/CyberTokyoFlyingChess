# 美术素材接入规范

这份规范定义"一张图从画完/生成，到出现在游戏里"的完整路径。**按这个规范出的图，
拖进对应文件夹 + 在查找表里挂上，就能进游戏，一行代码都不用改。**
无论是你自己画的还是 AI 生成的，都走同一条路。

## 一张图进游戏的三步

1. **按下面的表格出图**（尺寸/格式/留白要求）
2. **放进对应文件夹**（`Assets/Art/Sprites/<类别>/`），命名用后台的 wire 值（见下）
   —— 导入设置会由 `ArtImportPostprocessor` 自动套上，不用手动调 Inspector
3. **挂进查找表**：Project 窗口打开 `Assets/Resources/Data/` 下对应的资产
   （TileColorPalette / BuildingVisuals / CenterStateVisuals / RewardIcons / PieceVisuals），
   把图拖到对应条目的 Sprite 槽里

没挂图的条目自动退回占位表现（纯色/白点），所以素材可以一张一张换，随时能跑。

## 尺寸与格式

| 类别 | 文件夹 | 画布尺寸(px) | 锚点 | 说明 |
|---|---|---|---|---|
| 地格 | `Board/` | 128×128 | 居中 | 1 格 = 1 世界单位，四色各一张 |
| 棋子 | `Pieces/` | 96×96 | 底边中点 | 可爱动物飞行器，四色各一张 |
| 四角建筑 | `Buildings/` | 320×320 | 底边中点 | 占 3×3 角落空地，四栋各一张 |
| 中心哥斯拉 | `Center/` | 512×512 | 底边中点 | **四个状态一张一图，画布必须同尺寸、人物站位必须一致**，否则切状态时会跳位 |
| 奖励图标 | `Reward/` | 64×64 | 居中 | 五种奖励各一张 |
| UI | `UI/` | 按需 | 居中 | 按钮、面板等 |

- **格式一律 PNG，背景透明**。JPG 没有透明通道，不要用
- 立着的东西（棋子/建筑/哥斯拉）锚点在底边中点：脚踩地面处贴画布底边，
  这样"站在格子上"的定位换图不跳
- 不要把阴影画出画布边缘；投影要么画在画布内，要么后期用引擎做

## 命名：直接用后台的 wire 值

文件名里的标识符**逐字取自 fly-game-admin 后台定义的字符串**，不发明第二套名字：

```
tile_green.png  tile_yellow.png  tile_red.png  tile_blue.png
piece_green.png ...
building_startGate.png  building_tokyoTower.png  building_sensojiPagoda.png  building_luckyCatShrine.png
center_sleeping.png  center_angry.png  center_atomicBreath.png  center_pleased.png
reward_coin.png  reward_banknote.png  reward_dice.png  reward_card_shard.png  reward_mystery.png
```

## 给 AI 出图的提示词要点

- 主题：赛博朋克东京 × 怪兽（设计文档 `feixingqi/docs/design/` 有完整参考图）
- demo 里已有一套参考霓虹配色：粉 `#ff4d94`、青 `#35e6c4`
- 明确要求：透明背景（transparent background）、单个物体居中、指定输出尺寸
- 同一类别的多张图（比如四色棋子）**在同一次会话里生成**，风格才压得住
- 商用授权：留意所用工具的生成内容版权条款，上架前要能说清素材来源

## 查找表与代码的对应关系（改代码的人看）

| Resources 资产 | SO 类 | 消费方 |
|---|---|---|
| `Data/TileColorPalette` | `TileColorPaletteSO` | `TileView` |
| `Data/BuildingVisuals` | `BuildingVisualSO` | `BoardRenderer` |
| `Data/CenterStateVisuals` | `CenterStateVisualSO` | `CenterGodzillaController` |
| `Data/RewardIcons` | `RewardIconSO` | `TileView`（奖励标记） |
| `Data/PieceVisuals` | `PieceVisualSO` | `GameLoopController.SpawnPieces` |

这些资产必须待在 `Assets/Resources/` 下（运行时 Resources.Load，
场景序列化对这类引用有坑，见 BoardRenderer 头注释）。

以后素材上到后台素材库（R2）之后，中心状态会加一个远程加载版查找表
（走 `/api/game/asset`），本地这套作为兜底 —— 结构已为此预留。
