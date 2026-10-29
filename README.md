# DreamKeep · 梦境守望

> 单人卡牌策略 Roguelike · 计算机本科毕业设计 · 目标平台 PC

沉睡者的意识世界。你是**守梦人**——沉眠中唯一清醒的自我，梦魇正沿梦的走廊向觉醒边界压境。

---

## 环境要求

| 项       | 值                                      |
| ------- | -------------------------------------- |
| Unity   | `2022.3.62f3c1`（LTS 国内版 c1）            |
| 渲染管线    | Built-in                               |
| Git LFS | **必需**（美术 / 音频 / 动画资源走 LFS，未安装会导致签出失败） |
| IDE     | JetBrains Rider 或 Visual Studio 2022   |

> IDE 只做 IntelliSense / 重构 / 调试。**编译权永远归 Unity**——不要在 IDE 里 Build Solution 或 NuGet Restore。用 File > Open 打开项目根目录，不要打开 `.sln`。

## 技术栈

| 类别      | 选型                                                                          |
| ------- | --------------------------------------------------------------------------- |
| 依赖注入    | VContainer `1.19.0`                                                         |
| 异步      | UniTask                                                                     |
| 补间动画    | DOTween                                                                     |
| 2D 动画   | Spine                                                                       |
| 编辑器扩展   | NaughtyAttributes（仅编辑器绘制特性，运行时不参与）                                          |
| 多态配置序列化 | SerializeReferenceExtensions（`[SerializeReference]` + `[SubclassSelector]`） |
| UI      | uGUI、TextMeshPro                                                            |
| 输入      | Input System                                                                |
| 序列化     | Newtonsoft.Json `3.2.2`                                                     |
| 其他      | Timeline、PostProcessing、Visual Scripting                                    |

---

## 架构概览

一句话总纲：**十一个程序集、一张无环依赖 DAG、四通道解耦（命令上行 / 事件下行 / 只读查询 / 状态介入）；配置只读一次即烘焙为运行时对象，规则与状态只存在一处，表现层只读不回写。**

> 详细逐文件评审见 [Doc/架构分析报告.md](Doc/架构分析报告.md)（综合评分 **94 / 100**）。

### 程序集与依赖（asmdef 强制，已用 GUID 反解核验无环）

```
【叶层：仅依赖外部包】
  Utility        （纯 C#，零 UnityEngine 依赖）
  ConfigData     （纯资产，零执行逻辑）
        │                    │
        ▼                    ▼
  RuntimeData  ──→ {Utility, ConfigData}
        │
        ▼
  GameBattle   ──→ {Utility, RuntimeData, ConfigData}
        │
        ├──→ GameEvent   {RT, CD, UT}   空骨架（占位标记）
        ├──→ GameRest    {RT, CD, UT}   空骨架（占位标记）
        ├──→ GameShop    {RT, CD, UT}   空骨架（占位标记）
        │
        └──→ GameRun     {GameBattle, GameEvent, GameRest, GameShop, …}   ✅ 已实现（遗物系统）

【引擎级通用件】
  EngineUtility ──→ {Utility}
        │
        ▼
  CommonPresentation ──→ {全部下层}     ← 跨场景可复用表现件
        │
        ▼
  Presentation       ──→ {全部下层}     ← 战斗场景专属表现与装配
```

`ConfigData` 是纯资产/数据层，**不依赖 `Utility`**——实际是**扇形汇入**而非单链。全部程序集构成有向无环图，编译期即生效，无任何反向引用或环。

> **关于骨架程序集**：`GameEvent` / `GameRest` / `GameShop` 目前各只有 1 个 7 行占位标记，**尚无实质代码**，作用是提前锁死未来的依赖方向（战斗不认识跑图/休息/商店）。`GameRun` 已落地为完整遗物系统（12 文件 / 396 行），是"Battle ← Run"边界的首个真实消费者，零反向依赖。若你在此仓库上继续开发，请把三个空骨架视为"预留的空房间"而非已实现的系统。
>
> **✅ CommonPresentation 边界已澄清**：本轮遗物顶栏件（`UI_RelicsGroup` 等）已迁至 `Presentation/TopBar`，`CommonPresentation` 不再引用 `GameRun`，仅经 sanctioned 查询门面 `BattleQueryDispatcher` 引用 `GameBattle`，其"跨场景纯共享库"语义名副其实（详见架构报告 H-2 已解决）。

### 八个有实质代码的域

| 程序集                    | 职责                        | 关键内容                                                                                                  |
| ---------------------- | ------------------------- | ----------------------------------------------------------------------------------------------------- |
| **Utility**            | 纯 C# 通用件（零业务、零引擎依赖）       | 状态机 `StateMachine<T>`、确定性随机 `Rng`、可修正数值引擎 `ModifiableStat`、纯 C# 对象池 `ObjectPool<T>`                  |
| **EngineUtility**      | 引擎级通用件（依赖 UnityEngine，不含业务） | 组件池 `MonoObjectPool<T>`、贝塞尔 `Curve`、UI 几何 `UiGeometry`、补间 `UiTween`、悬停执行器 `UI_HoverOffset/Scaler/Active` |
| **ConfigData**         | 配置资产（策划可编辑，只有"做什么、做多少"）   | `SO_Player` / `SO_Enemy` / `SO_Card`、状态资产 `SO_Perk` + 5 个具体 `SO_*`、**遗物资产 `SO_Relic` + 9 个具体 `SO_*`**、**药水资产 `SO_Potion` + 8 个具体 `SO_*` + 2 枚举**、`AD_*` 词条 / `UD_*` 升级补丁 / 意图数据 |
| **RuntimeData**        | 运行时对象（纯 C#，配置只读一次即烘焙）    | `Creature` / `Player`（含 3 槽药水持有）/ `CardModel`(=`CM_Static`+`CM_Runtime`) / `CardPile`、`BattlePerk`+`BP_*`、`CardAffix`+`CA_*`、三个烘焙器、`EI_*` 意图 |
| **GameBattle**         | 战斗域（流程、规则、桥）              | 八阶段状态机、19 个 `BattleAction` + `ActionSystem`、状态介入层 `Hook`、三个 Resolver（含 `PotionResolver`）、命令/事件/查询三桥、分派守卫 |
| **GameRun**            | 跑图/局外域（**已落地**：遗物触发与编排）     | `RelicEffect`（订阅战斗事件+组合动作）、`RelicBaker`（按 `SO_Relic` 子类分派并登记 `IDeathGuard`）、`RelicTriggerNotifier`、9 个 `RE_*` 具体遗物 |
| **CommonPresentation** | 跨场景可复用表现件（**纯共享，已不含域 HUD**） | `UI_CardFace` + `UI_CardFaceSelector` + `CardValueRenderer` + `CardSorter`、`UI_HoverTip` + `PopFactory`、`UI_NumberPopTip`、`UI_OneShotVfx` + `VfxFactory` + `UI_PerkAppliedVfx` + `UI_PotionSplashVfx` |
| **Presentation**       | 战斗场景专属表现与装配（**含遗物顶栏与药水栏**） | 单位视图 / 卡牌视图 / HUD / 瞄准族（`IAimClient`+`BattleTargetDirector`）/ 遗物顶栏 `UI_RelicsGroup` 等 / 药水栏 `UI_PotionGroup` 等、演出编排、DI 组合根 `BattleLifetimeScope`、`BattleEntry`、`BattlePopRoot` / `BattleVfxRoot` |

### 四通道通信（表现层 ↔ 战斗逻辑）

```
① 上行·命令（玩家意图，只入队不执行）
   视图 → BattleCommandDispatcher → BattleLoop.EnqueueCommand → 等待态阶段取出执行

② 下行·事件（域侧结果，26 个 struct 只读载荷，零 GC）
   BattleAction.Execute → List<IBattleEvent> → BattleEventDispatcher.PlayAsync → 各视图处理器并行演出

③ 上行·只读查询（预览，无副作用）
   表现层 → BattleQueryDispatcher.(PreviewTargets / PreviewCardDamage / PreviewCardBlock /
                                   PreviewIntentDamage / PileCount / PileCards / GetAliveEnemies)
          → 直算返回（与结算共用同一份规则）

④ 横向·状态介入（规则层内）
   BA_Damage / BA_Block → Hook.Resolve(...) 求最终值；BA_SettlePerk → Hook.Settle(...) 收集状态结算动作
```

**通道 ③ 与 ④ 共用同一份 `Hook.Resolve`**，因此"卡面预览的伤害"与"实际扣掉的血"在结构上不可能不一致。

### 核心纪律（新增代码请沿用）

- **状态变更只经动作系统。** `Creature` / `CardPile` / `PlayerCombatState` 的所有写操作都在 `BattleAction.Execute` 里。
- **视图只读不回写。** 表现层不持有业务规则、不直接调阶段方法。
- **配置只读一次。** 数值在烘焙期抽成运行时快照，运行时对象不持有配置引用。
- **失败路径也走事件。** 能量不足 / 卡已离场 / 带「不能打出」词条 → `BE_CardPlayRejected`，不静默丢弃。
- **事件一律 `struct` + 只读载荷**，零 GC。
- **状态数值唯一入口是 `Hook`。** 新增状态只需在 `BattlePerk` 子类实现 `GetModifier` 并在 `Hook` 登记分支。
- **预览与结算共用规则。** 卡面/瞄准的数值预览经 `BattleQueryDispatcher` 现算，绝不另写一份判定。
- **DI 组合根是唯一允许同时认识配置资产与运行时对象的地方。**
- **手牌区四组件各司其职。** `UI_HandView`（编排 + 每帧驱动）/ `UI_HandHover`（悬停裁决）/ `UI_HandAim`（瞄准客户端 + 数值预览）/ `UI_HandShow`（事件演出）同节点协作。
- **可复用件放对层。** 只依赖 UnityEngine 的通用工具 → `EngineUtility`；跨场景复用的表现件 → `CommonPresentation`；只有战斗才用的 → `Presentation`。

### 代码规模（2026-10-25 晚间工作树，`HEAD = 0e97bc6`）

| 域                   |      文件 |       行数 |
| ------------------- | ------: | -------: |
| Utility             |       7 |      230 |
| ConfigData          |      61 |      959 |
| RuntimeData         |      40 |     1527 |
| GameBattle          |      75 |     2311 |
| GameEvent / GameRest / GameShop（骨架） | 3 | 21 |
| GameRun             |      12 |      396 |
| EngineUtility       |       9 |      496 |
| CommonPresentation  |      14 |     1379 |
| Presentation        |      49 |     4836 |
| **合计**              | **270** | **12155** |

257 / 270 个文件在 150 行以内（95.2%）。超出的 13 个均为内聚单体：`UI_HandView` 454 / `UI_CardBattle` 366 / `UI_PotionGroup` 348 / `UI_CardFace` 257 / `UI_HandHover` 227 / `UI_HandShow` 218 / `UI_RelicSlot` 186 / `UI_Actor` 183 / `BattleTargetDirector` 178 / `UI_HoverTip` 168 / `CM_Runtime` 167 / `UI_PileCardsView` 164 / `CardModelBaker` 163。

---

## 当前开发状态

### 已完成并可运行

**战斗最小闭环（纵向切片已通）**

- 八阶段回合循环（战斗开始 → 玩家起 → 玩家操作 → 玩家结 → 敌方起 → 敌方行动 → 敌方结 → 战斗结束）
- 卡牌完整生命周期：抽牌 → 手牌 → 拖拽/瞄准 → 打出堆 → 词条结算 → 弃牌/消耗/遗忘离场
- 五个牌堆（抽牌堆 / 手牌 / 打出堆 / 弃牌堆 / 消耗堆）+ 局内卡组跨战斗保留
- 敌人意图系统：意图配置 → 烘焙 → 运行时意图 → 头顶图标预告 → 执行 → 推进下一条；站最前槽位自动替换意图
- 纵深阵型：0 号玩家 + 1~3 号敌人槽位，敌人可前进 / 回绕，槽位占用判定
- 状态（Perk）系统：数据驱动（`SO_Perk` + 5 个具体资产，`PerkBaker` 烘焙）；结算时机声明（回合开始 / 回合结束 / 常驻）、施加与叠加、先触发效果再递减层数、归零移除
- 状态数值接入结算：力量/敏捷经 `Hook` 修正伤害与格挡，虚弱/易伤参与乘性修正；再生在回合边界经 `Hook.Settle` → `BA_Heal` 结算（敌我共用）
- 卡牌升级全链路：`UD_*` 配置 → `CardModelBaker` 解析为 `CM_ValuePatch` → `CM_Runtime.Upgrade()` 现改数值 / 追加词条 / 移除词条
- 遗物系统（跑图域 `GameRun`）：`SO_Relic` + 9 个具体遗物资产；`RE_*` 触发效果经**订阅战斗事件 + 组合 `BattleAction`** 接入（零反向依赖），`LizardTail` 经 `IDeathGuard` 接入致命伤拦截；`RelicBaker` 按资产类型分派并登记遗物。战斗内已可触发
- **药水系统（战斗内可消耗）**：`SO_Potion` + 8 个具体药水资产（含回血/格挡/能量/灼烧/力量/虚弱/束缚等）；饮用经 `BC_UsePotion` 命令 → `PotionResolver` 解析为战斗动作 → `BA_ConsumePotion` 取走并广播 `BE_PotionConsumed`，落点规则全在 Resolver、表现层只读不重算；玩家运行时持 3 槽药水（`Player.TryTakePotion`）。丢弃走即时旁路（无战斗效果）

**表现层（已成型）**

- Spine 动画演出：攻击 / 受击 / 死亡 / 施法，播到 50% 即放行事件链（非全阻塞并行）
- 手牌区：弧形排布（以"手牌区的点"为基准，拖点即整手挪位）、悬停原位偏移 + 放大 + 两侧避让、拖拽阻尼跟随、瞄准定住
- 拖拽瞄准：目标箭头沿带弧曲线排布、命中整支变红 + 头部弹一次；落点规则与结算同源
- 牌堆：三堆计数逐张步进 + 进堆弹跳、点击弹出查看面板（抽牌堆按类型陈列，弃牌/消耗堆按进入顺序）
- 弹层与特效：伤害/回血数字飘字（Rise / Toss 双形态）、受击与回复一次性特效、说明气泡（双触发模式 + 整列叠排）、回合横幅
- **状态特效**：状态施加爆发演出（`BE_PerkApplied → BattleVfxRoot → VfxFactory.PlayPerk → UI_PerkAppliedVfx`，增益染绿减益染红）与常驻状态图标（`UI_PerkGroup` 池化 `UI_PerkSlot`），复用既有事件驱动/池化管线，无平行机制
- 卡面：四张内嵌牌面按类型切换、稀有度换边框三图、不可打整卡压暗、数值修正着色（升绿降红）、关键词行 + 悬停说明气泡

**已有内容资产**

- 卡牌 8 张（Attack / Bash / Block / ForgottenRitual / PommelStrike / SwordBoomerang / Uppercut / Vicious）
- 敌人 1 种（树枝史莱姆 TwigSlime，四条意图 + 最前槽位替换意图）、玩家 1 套
- 状态 5 种（再生 / 力量 / 敏捷 / 易伤 / 虚弱，均数据驱动）
- 词条 10 种 + 升级补丁 4 种 + 意图 6 种
- 遗物 9 种（Anchor / BurningBlood / CentennialPuzzle / GremlinHorn / HappyFlower / LizardTail / Orichalcum / Pendulum / Shuriken，战斗内效果已通）
- 药水 8 种（Block / Blood / Energy / Fire / Strength / Weak / PotionOfBinding 等，战斗内饮用已通）
- 预制体 25 个（卡面 4 种、意图图标 5 种、单位 2 种、弹层 4 种、特效 2 种、面板若干）

### 近期落地（2026-10 下旬）

- **拆出 `EngineUtility` 与 `CommonPresentation`**：可复用边界首次从"战斗场景"下移到"引擎级 + 跨场景表现级"，新场景（跑图 / 商店）不必复制表现件。
- **`GameRun` 遗物系统落地**：从 7 行占位标记落地为完整遗物域（12 文件 / 396 行）；`RE_*` 经战斗事件订阅 + 动作组合接入、`RelicBaker` 按 `SO_Relic` 子类分派、`LizardTail` 经 `IDeathGuard` 介入致命伤。`CommonPresentation` 同步新增遗物顶栏展示件（`UI_RelicsGroup` / `UI_RelicSlot` / `UI_RelicsInfoPanel` / `UI_TopInfoBar`）。
- **牌堆查看面板**：三堆共用一个变体，打开期间订阅事件做池化差分对账。
- **悬停组件化**：`UI_HoverOffset` / `UI_HoverScaler` / `UI_HoverActive` 抽出为"一条补间、两种触发"的双模式执行器，按钮与手牌共用同一份实现。
- **六原则重构（当帧结算 / 瞄准直连 / 悬停组件化）**：`BattleLoop` 胜负判定当帧生效；瞄准结果由中间件直接回写瞄准客户端，不再经手牌区中转。
- **受击 / 回复特效与卡牌边缘涟漪**：`UI_OneShotVfx` 自识别粒子或帧动画，播完还池；移除 ShaderGraph 依赖。

### 近期落地（2026-10-25 晚间）

- **药水系统落地**：`ConfigData/Potion` 配置（8 具体 `SO_*` + 2 枚举）+ `GameBattle` 命令/事件/动作/解析器（`BC_UsePotion`/`BE_PotionConsumed`/`BA_ConsumePotion`/`PotionResolver`）+ 玩家运行时 3 槽药水持有 + `Presentation` 药水栏（4 文件 626 行）。严格遵循四通道 + SO 驱动，与卡牌/遗物同构、零反模式。
- **状态特效**：`BE_PerkApplied` 经 `BattleVfxRoot` → `VfxFactory.PlayPerk` → `UI_PerkAppliedVfx` 演出状态施加爆发，常驻图标走 `UI_PerkGroup` 池化 `UI_PerkSlot`；复用既有事件驱动/池化管线，无平行机制。
- **分派守卫 + `IAimClient`**：Resolver 补 `default:throw` 失败即抛（漏登记类型不再静默失效）；`BattleLoop`/`ActionSystem` 加重入/终态守卫；抽出 `IAimClient` 接口使 `BattleTargetDirector` 依赖倒置（手牌/药水均可作瞄准客户端）。遗物顶栏从 `CommonPresentation` 迁至 `Presentation`，解决此前 H-2 耦合问题。

### 已知未完成（功能完整性，非架构缺陷）

| 项 | 状态 |
| --- | --- |
| **战斗结束奖励** | **胜利分支已打通**：`SO_EncounterReward`（挂在 `SO_Encounter` 上，Boss 不配）+ 三个池 → `EncounterRewardRoller` 掷清单 → `UI_RewardPanel` 自己铺条目并兑现（金币 / 卡牌三选一 / 概率药水 / 精英遗物）→ 继续开地图占位并卸段。**战败分支仍未接**（照旧立即收段） |
| 卡牌升级正式入口 | 升级数据 → 烘焙 → 运行时应用全链路已通；但"升级获取途径 / 升级 UI 入口"（事件 / 遗物 / 商店）仍未规划，玩家当前无正式方式触发升级 |
| 商店 / 金币 | `SO_Player._startGold` 已定义，零消费 |
| 遗物（局外持有 / 掉落 / 跨战斗生效） | 战斗内效果已通过 `GameRun` 接通，但遗物的获取 / 持有 / 掉落与跑图进度循环未做；`Player` 仍记名 7 项待落地系统（金币 / 遗物 / 药水 / 法球 / 扩展字段 / 难度 / 抽取袋） |
| 药水（局外持有 / 掉落 / 获取途径） | 战斗内饮用 / 丢弃 / 效果结算已通；**战斗奖励可掉落药水**（`Player.TryAddPotion` + 槽位变更事件），且战斗外也能丢弃、能喝"随时可用"的药水（`SO_Potion.UsableOutsideBattle`，目前仅鲜血药）。商店 / 事件等其它获取途径未做 |
| 格挡 / 治疗意图数值 | 数据层有 `Amount`，意图图标均为"纯图标占位"，玩家无法预判敌人会挡多少 / 回多少 |
| 音效 | 代码中已留触发点 TODO（发牌、进堆），音效层尚未开写 |
| 单局内的地图 / 楼层 / 敌人编排 | 战斗是唯一场景（`Scene_Battle.unity`），跑图系统未开写 |
| `BattleEventDispatcher.Unregister` | 单位视图销毁后事件处理器仍留在字典（单场战斗内有界滞留，靠表现层 null 检查兜住，无实际 bug） |

---

## 后续计划

> 按依赖顺序排列，前项未完成时后项会返工。单人开发，按"先让已有数据生效、再扩系统"推进。

### 阶段一：收口当前重构（已完成 ✅）

1. ✅ 手牌区四组件各司其职（编排 / 悬停裁决 / 瞄准 / 演出）。
2. ✅ 通用对象池（`ObjectPool<T>` + `MonoObjectPool<T>`）与可复用表现件抽取。
3. ✅ 状态数值系统闭环（`Hook` + 重写 `ModifiableStat`）。
4. ✅ 卡牌升级子系统全链路打通。
5. ✅ `EngineUtility` / `CommonPresentation` 拆出，跨场景复用边界建立。

### 阶段二：补齐战斗内的功能空洞（最高优先级）

0. **（架构决议，先于功能开发）三个骨架程序集的去留**：`GameEvent` / `GameRest` / `GameShop` 现仍为 7 行空标记，且 `CommonPresentation` / `Presentation` / `GameRun` 的 asmdef 各声明了对它们的引用而全项目使用次数为 0（`GameRun` 自身已落地，不再属于骨架）。二选一——删掉三个空骨架及对应前向引用，或保留但显式标注"预声明、暂无消费者"。详见证报告 H-1。**（H-2 已解决）** 遗物顶栏已迁至 `Presentation`，`CommonPresentation` 不再耦合 `GameRun`，其"纯共享库"语义已恢复。
1. **战斗结束奖励流程**：胜利 / 失败分支、奖励展示与选择、结果落回局内状态。
2. `BattleEventDispatcher` 增 `Unregister`，单位视图在 `OnDestroy` 反注册事件处理器。
3. 敌方意图图标补数值展示（与卡面 `CardValueRenderer` 同源）。
4. 升级正式入口：升级 UI + 获取途径（事件 / 遗物 / 商店三选一）。
5. 音效层 `Presentation/Feedback`：订阅 `BE_*` 播发声，不在视图里直调。

### 阶段三：可扩展性加固

1. `PerkBaker` / `CardModelBaker` / `IntentBaker` / `Hook.Settle` 的 `switch` 加 `default` 报错，把"忘加 case"从静默失效变成可见失败（Resolver 三处已先行补齐 `default:throw`，见架构报告 M-3 部分解决）。
2. ~~`BattleTargetDirector` 抽 `IAimClient` 接口~~ —— **已完成**：`IAimClient` 已抽出，`BattleTargetDirector` 现依赖接口，`UI_HandAim`/`UI_PotionGroup` 均可实现（M-1 已解决）。
3. 清理三处未使用依赖（`Utility → UniTask`、`Presentation → Unity.Mathematics`、组合根里无消费者的 `RegisterInstance(battleContext)`）。
4. 阵型上限（`MaxEnemyCount`）收进 `BattleSlots`，与槽位定义同源。
5. `UI_CardBattle` 再切一刀：姿态通道与补间通道合并为 `CardPose`。
6. （新增）分派守卫为手抄惯用法，建议抽一层微型 `ReentrancyGuard`/`RunOnceGuard` 并区分 `IDeathGuard` 命名，避免随异步入口增多继续复制（架构报告 M-6）。

### 阶段四：新系统（视毕设进度决定是否开写）

1. **局内地图与楼层编排**：在 `GameRun` 程序集内实现，方向 `GameRun → GameBattle`（战斗不认识跑图）。
2. **遗物系统（局外闭环）**：战斗内效果已通（`GameRun`），下一步做遗物的获取 / 持有 / 掉落与跑图进度循环，作为局内成长的主承载。
3. **商店与金币**：消费端已就位（金币字段、牌堆视图可复用）。

---

## 快速开始

```bash
git clone https://github.com/mzyqzmd/DreamKeep.git
cd DreamKeep
git lfs install   # 首次必须，否则签出的是 LFS 指针文件而非真实资源
git lfs pull
```

1. Unity Hub → `Add project from disk` → 选择本仓库根目录
2. 等待 `Library/` 首次导入完成
3. 打开 `Assets/Scenes/Scene_Battle.unity` 运行

---

## 文档

| 文档 | 内容 |
| --- | --- |
| [Doc/架构分析报告.md](Doc/架构分析报告.md) | 逐文件架构评审：综合评分 94/100、六维分项评分、逐模块分析、反模式扫描、不变量核验、问题清单 |
| [Doc/世界观与背景设定.md](Doc/世界观与背景设定.md) | 游戏世界观与背景设定文档 |
| [.codebuddy/Docs/03-毕业设计方案-卡牌肉鸽融合成熟玩法.md](.codebuddy/Docs/03-毕业设计方案-卡牌肉鸽融合成熟玩法.md) | 纯游戏设计稿（v3.5）：玩法 / 数值 / 题材定案，不含实现 |
| [.codebuddy/Docs/04-项目实现顺序与系统解耦设计.md](.codebuddy/Docs/04-项目实现顺序与系统解耦设计.md) | 架构定案与 P0 待办清单：「代码放哪、依赖谁、先做什么」以此为准 |

> `.codebuddy/` 为本地 AI 辅助目录，已在 `.gitignore` 中忽略，不进仓库。上表两个 Docs 链接仅供本地查阅。

---

## 仓库约定

- `Assets/_Art/`、`Assets/_Data/`、`Assets/_Prefabs/`、`Assets/_Scripts/` 为项目自有内容
- `Assets/Plugins/`、`Assets/Editor/`、`Assets/Resources/` 为第三方插件与工具集
- 新增玩法代码一律放在 `Assets/_Scripts/<域>/` 下对应位置，**不得在 `Assets/` 根目录或其他目录散落 .cs 文件**（否则会落进 `Assembly-CSharp` 而脱离 asmdef 边界）
- 每个域一个 `asmdef`（`_DreamKeep.<域>.asmdef`），依赖方向必须与本文档的 DAG 一致
- 提交前用 `git lfs ls-files` 确认本次新增的二进制资源已被 LFS 接管
- 编码规范：private/protected 字段 `_camelCase`、枚举 `E` 前缀、UI 类 `UI_` 前缀、SO 类 `SO_` 前缀、public 方法用 `#region` 包裹、using 放 namespace 外
- 注释写"为什么"和"踩过什么坑"，不写"做了什么"。**重构时不要删注释**——本项目的注释密度是其最有价值的部分
