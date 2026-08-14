# AI 出图提示词（配合 docs/art-spec.md 使用）

用法：把每段英文提示词贴进出图工具（推荐 ChatGPT 图像生成，**支持透明背景**；
Midjourney 质量高但要自己抠底）。出来的图按 art-spec 的命名放进
`Assets/Art/Sprites/<类别>/`，再在 `Assets/Resources/Data/` 的查找表里拖上即可进游戏。

通用要求（每段提示词都要带上，这里不重复写）：

> transparent background, single object centered, clean silhouette,
> mobile game asset, cyberpunk neon Tokyo theme, night palette with
> neon pink (#ff4d94) and neon cyan (#35e6c4) accents

**同一类别的多张图在同一次会话里连续生成**，风格才统一。

---

## 地格（4 张，128×128，`Board/tile_<color>.png`）

> Isometric diamond-shaped floor tile for a board game, 2:1 isometric
> perspective, glowing neon edge, dark asphalt surface with subtle
> holographic circuit pattern, dominant color: {vivid green / bright
> yellow / hot pink-red / electric blue}, slight 3D thickness on the
> bottom edge

四次生成分别替换大括号里的颜色。注意：出来的图如果是正方形画布里的菱形，
画布裁到菱形贴边即可（引擎按 128×64 的 2:1 菱形摆放，图里菱形本体比例要对）。

## 棋子（4 张，96×96，`Pieces/piece_<color>.png`）

设计文档主题是"可爱动物飞行器"：

> Cute chibi animal aviator in a tiny hovering vehicle, {green frog /
> yellow duck / pink-red cat / blue penguin} pilot wearing goggles,
> small jet flames underneath, front-facing 3/4 view, bold outline,
> sticker style, feet/vehicle bottom touching the bottom edge of canvas

## 四角建筑（4 张，320×320，`Buildings/building_<id>.png`）

> Cyberpunk neon miniature of {Tokyo Tower / Sensoji temple pagoda /
> lucky cat (maneki-neko) shrine / traditional torii gate as a start
> gate}, isometric 3/4 view, glowing neon signs and holographic
> billboards, dark metal and glass, building base touching the bottom
> edge of canvas

id 对应：`tokyoTower` / `sensojiPagoda` / `luckyCatShrine` / `startGate`。

## 中心哥斯拉（4 张，512×512，`Center/center_<state>.png`）

⚠️ 四张必须同画布、同站位、同大小，只换表情/姿态 —— 生成时先出 sleeping，
然后在同一会话里说"same character, same pose and position, but ..."：

> A kaiju monster (Godzilla-like) standing upright in the center of a
> neon cyberpunk city board, chibi proportions, dark scales with neon
> cyan dorsal fins, feet touching the bottom edge of canvas
> - sleeping: eyes closed, peaceful, dim glow
> - angry: eyes glowing red, roaring, fins flashing bright
> - atomicBreath: mouth open firing a vertical energy beam upward, fins blazing
> - pleased: content smile, soft warm glow, relaxed posture

## 奖励图标（5 张，64×64，`Reward/reward_<kind>.png`）

> Flat game reward icon with neon glow rim, {gold coin / stack of
> banknotes / white six-sided die / glowing card fragment / purple
> mystery box with question mark}, centered, simple bold shape readable
> at small size

kind 对应：`coin` / `banknote` / `dice` / `card_shard` / `mystery`。

---

## 挂图后的验证

任何一张图挂上后直接 Play：对应元素立即换成新图，其余保持占位。
四张哥斯拉全挂上后，踩箭头格进中心触发状态切换，检查四张图切换时不跳位
（跳位说明画布/站位不一致，回炉重出）。
