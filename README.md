# Cyber Tokyo Flying Chess (Unity)

赛博东京飞行棋，Unity 2D (URP) 客户端。学习向 + 目标是做成可上架 iOS/Android 的产品，不是 demo。

## 与另外两个项目的关系

- **`fly-game-admin`**（`../feixingqi`）—— Cloudflare Worker + D1 的配置后台，**棋盘/奖励规则的唯一来源**。
  本项目的 `Assets/Scripts/Core/Board`、`Core/Reward` 是它 `src/geometry.ts`、`src/types.ts`、
  `src/defaults.ts` 的 C# 移植，改动前先去看那边。它的 `/api/game/config` 是本项目的配置来源
  （见 Phase 4），本地跑它：`cd ../feixingqi && npm run dev`。
- **`fly-game-app`**（不在本机，Cocos Creator 3.8.8）—— 另一个客户端，目前还没接 `fly-game-admin`
  的配置接口，仍用本地 `generateBoard()`。本项目与它是并行关系，不是替代或依赖。

## 目录结构

```
Assets/
├── Art/            美术资源（占位阶段用纯色/几何图形，见下方素材接入规范）
├── Audio/
├── Prefabs/
├── Data/           ScriptableObject 资产（默认配置、美术查找表）
├── Scenes/
├── Scripts/
│   ├── Core/       纯 C#，不依赖 UnityEngine（Board/Reward/State）—— 对应后台 core 的「零依赖、可测」原则
│   ├── Networking/
│   ├── Gameplay/
│   ├── UI/
│   └── Editor/     批处理可跑的编辑器脚本（如默认配置生成器）
└── Tests/EditMode/ 几何/规则的单元测试，含与后台对账的 fixture
```

## 未决项（继承自后台，客户端先用占位规则顶上，标了 TODO 方便以后替换）

| Tag | 内容 |
|---|---|
| OPEN-1 | 48 格里哪几格带奖励 |
| OPEN-2 | 哥斯拉中心状态切换的触发条件 |
| OPEN-3 | 棋子从传送带抵达中心之后怎么办 |
| OPEN-4 | 自由传送格的目的地规则 |
| OPEN-5 | 四角建筑踩上去的效果 |
| OPEN-6 | 胜利条件 / 一局怎么算结束 |

**接手本项目请先读 `docs/HANDOFF.md`** —— 开发全过程、关键决策与坑、构建命令、待办与出图清单都在那里。
