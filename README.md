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

一句话总纲：**八个程序集、一张无环依赖 DAG、四通道解耦（命令上行 / 事件下行 / 只读查询 / 状态介入）；配置只读一次即烘焙为运行时对象，规则与状态只存在一处；表现层与战斗逻辑经四通道解耦，奖励兑现等运行期写入口经显式注入的 `RunState` 引用完成（不绕过动作系统）；多场景阶段（先古之民段 / 休息段 / 战斗段）由整局层（RunState）统一持有进度、按场景名叠加编排。**

> 详细逐文件评审见 [Doc/架构分析报告.md](Doc/架构分析报告.md)（综合评分 **94 / 100**）。

### 程序集与依赖（asmdef 强制，已用 GUID 反解核验无环）

```
【叶层 / 域层：仅依赖外部包或下层域】
  Utility        （纯 C#，零 UnityEngine / 零项目内依赖）
  ConfigData     （纯资产，零执行逻辑；外部：NaughtyAttributes, SerializeReferenceExtensions）
  RuntimeData    ──→ {Utility, ConfigData}
  EngineUtility  ──→ {UniTask, DOTween}          （已无 Utility 依赖，纯引擎件）
  GameBattle     ──→ {Utility, RuntimeData, ConfigData} + UniTask
  GameRun        ──→ {GameBattle, RuntimeData, ConfigData, Utility} + UniTask   ✅ 已实现（遗物 52 / 奖励 / 整局 RunState）

【表现层（平级两层；Presentation 依赖 CommonPresentation 为故意设计）】
  CommonPresentation ──→ {Utility, ConfigData, RuntimeData, EngineUtility, GameBattle, GameRun} + 外部包（VContainer/UniTask/TMP/NaughtyAttributes/DOTween）
  Presentation        ──→ {CommonPresentation + 上述全部域层} + 外部包（VContainer/UniTask/TMP/InputSystem/Spine/NaughtyAttributes/DOTween）
```

> 注：`CommonPresentation` 与 `Presentation` 为**平级的表现层**——通用表现件集中在 `CommonPresentation`，各场景阶段专属表现放 `Presentation`；`Presentation` 依赖 `CommonPresentation` 是故意设计。`CommonPresentation → GameRun` 是表现层对域层的正常向下依赖，`UI_RewardPanel` 经注入的 `RunState` 兑现奖励在允许方向上，**非架构缺陷**（往期曾被误判为 H-2"越界写耦合"，经澄清不成立）。

`ConfigData` 是纯资产/数据层，不依赖 `Utility`——实际是扇形汇入而非单链。全部程序集构成有向无环图，编译期即生效，无任何反向引用或环。

> **关于空骨架程序集**：`GameEvent` / `GameRest` / `GameShop` 三个空骨架 asmdef **已于本轮删除**（H-1 彻底解决），依赖图不再表达"比实际需要更多"的约束。当前 8 个 asmdef 全部有实质代码。

### 八个有实质代码的域

| 程序集                    | 职责                        | 关键内容                                                                                                  |
| ---------------------- | ------------------------- | ----------------------------------------------------------------------------------------------------- |
| **Utility**            | 纯 C# 通用件（零业务、零引擎依赖）       | 状态机 `StateMachine<T>`、确定性随机 `Rng`、可修正数值引擎 `ModifiableStat`、纯 C# 对象池 `ObjectPool<T>`                  |
| **EngineUtility**      | 引擎级通用件（依赖 UnityEngine，不含业务） | 组件池 `MonoObjectPool<T>`、贝塞尔 `Curve`、UI 几何 `UiGeometry`、补间 `UiTween`、悬停执行器、按钮/面板抽象基类（已无 `Utility` 依赖）           |
| **ConfigData**         | 配置资产（策划可编辑，只有"做什么、做多少"）   | `SO_Player` / `SO_Enemy` / `SO_Card`、状态资产、`AD_*` 词条 / `UD_*` 升级补丁 / 意图数据、**遗物 `SO_Relic` + 52 具体 `SO_*`**、**药水 `SO_Potion` + 15 具体 `SO_*`**、**Ancient（先古之民/事件）5 文件**、**Run/Encounter+Background 6 文件** |
| **RuntimeData**        | 运行时对象（纯 C#，配置只读一次即烘焙）    | `Creature` / `Player`（3 槽药水 + 持卡组/遗物引用）/ `CardModel`(=`CM_Static`+`CM_Runtime`+`CM_ValuePatch`) / `CardPile`、`BattlePerk`+`BP_*`、`CardAffix`+`CA_*`、三个烘焙器、`EI_*` 意图 |
| **GameBattle**         | 战斗域（流程、规则、桥）              | 八阶段状态机、20 个 `BattleAction` + `ActionSystem`、状态介入层 `Hook`、三个 Resolver、命令/事件/查询三桥、分派守卫、奖励注入接线          |
| **GameRun**            | 跑图/局外域（**已大幅落地**）           | `RelicEffect`（订阅战斗事件+组合动作）+ `RelicBaker`（按 `SO_Relic` 子类分派并登记 `IDeathGuard`）+ 51 个 `RE_*`；`Reward`（`ERewardKind`/`RunReward`/`EncounterRewardRoller`）；`Run`（`RunState` 整局进度 + 事件下行） |
| **CommonPresentation** | 通用表现层（与 `Presentation` **平级**，被其依赖） | `UI_CardFace` + `UI_CardFaceSelector` + `UI_ConcreteCardView` + `UI_CardSelect` + `CardValueRenderer` + `CardSorter`、`UI_HoverTip` + `PopFactory`、`UI_NumberPopTip`、VFX 族（含 `UI_SmithForgeVfx`）、`UI_RewardPanel`（经注入 `RunState` 兑现奖励，表现层→域的正常方向） |
| **Presentation**       | 各场景阶段专属表现与装配（**三新阶段 + 整局层**） | Battle（单位/卡牌/HUD/瞄准/弹层特效/奖励泊入）；**Ancient 段**（Dialogue/Option/Root + Bootstrap）；**Rest 段**（Option/UI_SmithPanel/UI_RestVFXRoot + Bootstrap）；**Run 整局层**（TopBar：DeckPanel/MapPanel/MenuPanel/LayerBar/Relics/Potion + `SegmentSceneManager` 阶段编排）；DI 组合根 `BattleLifetimeScope`/`AncientLifetimeScope`/`RestLifetimeScope`/`RunLifetimeScope` |

### 四通道通信（表现层 ↔ 战斗逻辑）

```
① 上行·命令（玩家意图，只入队不执行）
   视图 → BattleCommandDispatcher → BattleLoop.EnqueueCommand → 等待态阶段取出执行

② 下行·事件（域侧结果，27 个 struct 只读载荷，零 GC）
   BattleAction.Execute → List<IBattleEvent> → BattleEventDispatcher.PlayAsync → 各视图处理器并行演出

③ 上行·只读查询（预览，无副作用）
   表现层 → BattleQueryDispatcher.(PreviewTargets / PreviewCardDamage / PreviewCardBlock / PreviewIntentDamage / PileCount / PileCards / GetAliveEnemies)
          → 直算返回（与结算共用同一份规则）

④ 横向·状态介入（规则层内）
   BA_Damage / BA_Block → Hook.Resolve(...) 求最终值；BA_SettlePerk → Hook.Settle(...) 收集状态结算动作
```

**通道 ③ 与 ④ 共用同一份 `Hook.Resolve`**，因此"卡面预览的伤害"与"实际扣掉的血"在结构上不可能不一致。三大新场景阶段（Ancient/Rest/Run）的表现层同样严格走命令/事件/查询三路上行下行，未引入平行机制。

### 多场景阶段与整局层（本轮新增）

- **整局层 `RunState`**（`GameRun/Run`，纯 C#）：持有 `Floor`/`Rng`/`Player` 的遗物·药水·卡组等整局状态；数值变更以 `event Action` 下行通知 HUD。
- **阶段编排 `SegmentSceneManager`**（`Presentation/_Bootstrap/Run`，245 行）：按**场景名字符串**（`Scene_Ancient`/`Scene_Rest`/`Scene_Battle`）Additive 加载/卸载阶段，无对下一阶段类型的硬引用；整局常驻靠 `RunLifetimeScope` + 叠加场景实现，**无 `DontDestroyOnLoad`**。
- **四个 `*LifetimeScope`**（Ancient/Rest/Run + Battle）：结构一致，`Configure` 内只装配本阶段类型，各段 scope 互不引用。

### 核心纪律（新增代码请沿用）

- **状态变更只经动作系统。** `Creature` / `CardPile` / `PlayerCombatState` 的所有写操作都在 `BattleAction.Execute` 里。
- **视图不持有业务规则、不直接篡改战斗状态。** 战斗状态写入只经动作系统；`RunState` 的奖励兑现经显式注入引用完成，属受控写入口（不绕过动作系统）。
- **配置只读一次。** 数值在烘焙期抽成运行时快照，运行时对象不持有配置引用。
- **失败路径也走事件。** 能量不足 / 卡已离场 / 带「不能打出」词条 → `BE_CardPlayRejected`，不静默丢弃。
- **事件一律 `struct` + 只读载荷**，零 GC。
- **状态数值唯一入口是 `Hook`。** 新增状态只需在 `BattlePerk` 子类实现 `GetModifier` 并在 `Hook` 登记分支。
- **预览与结算共用规则。** 卡面/瞄准的数值预览经 `BattleQueryDispatcher` 现算，绝不另写一份判定。
- **DI 组合根是唯一允许同时认识配置资产与运行时对象的地方。**
- **可复用件放对层。** 只依赖 UnityEngine 的通用工具 → `EngineUtility`；跨场景复用的表现件 → `CommonPresentation`；只有某阶段专属的 → `Presentation/<阶段>/`。`CommonPresentation` 与 `Presentation` 为平级表现层，`Presentation` 依赖 `CommonPresentation` 是故意设计。
- **整局进度由 `RunState` 持有，表现层只读。** 新场景 UI 一律 `[Inject] RunState`，不自行改写（奖励兑现等受控写入口除外）。

### 代码规模（2026-10-31 工作树，`HEAD = e2e10eb`）

| 域                   |      文件 |       行数 |
| ------------------- | ------: | -------: |
| Utility             |       7 |      237 |
| ConfigData          |     127 |     2022 |
| RuntimeData         |      42 |     1656 |
| EngineUtility       |      14 |      710 |
| GameBattle          |      77 |     2479 |
| GameRun             |      59 |     2074 |
| CommonPresentation  |      25 |     2586 |
| Presentation        |      74 |     6870 |
| **合计**              | **425** | **18634** |

406 / 425 个文件在 150 行以内（95.5%）。超出的 19 个均为内聚单体：`UI_HandView` 451 / `UI_PotionGroup` 400 / `UI_CardBattle` 366 / `UI_CardFace` 251 / `SegmentSceneManager` 245 / `UI_SmithForgeVfx` 243 / `UI_HandHover` 227 / `UI_HandShow` 218 / `UI_HoverTip` 210 / `UI_Actor` 198 / `UI_RelicSlot` 194 / `UI_RewardPanel` 192 / `RestEntry` 183 / `BattleTargetDirector` 178 / `UI_PileCardsView` 164 / `UI_DeckPanel` 163 / `CardModelBaker` 160 / `CM_Runtime` 156。

---

## 当前开发状态

### 已完成并可运行

**战斗最小闭环（纵向切片已通）**

- 八阶段回合循环（战斗开始 → 玩家起 → 玩家操作 → 玩家结 → 敌方起 → 敌方行动 → 敌方结 → 战斗结束）
- 卡牌完整生命周期：抽牌 → 手牌 → 拖拽/瞄准 → 打出堆 → 词条结算 → 弃牌/消耗/遗忘离场
- 五个牌堆（抽牌堆 / 手牌 / 打出堆 / 弃牌堆 / 消耗堆）+ 局内卡组跨战斗保留
- 敌人意图系统：意图配置 → 烘焙 → 运行时意图 → 头顶图标预告 → 执行 → 推进下一条；站最前槽位自动替换意图
- 纵深阵型：0 号玩家 + 1~3 号敌人槽位，敌人可前进 / 回绕，槽位占用判定
- 状态（Perk）系统：数据驱动（`SO_Perk` + 5 个具体资产，`PerkBaker` 烘焙）；结算时机声明、施加叠加、先触发后递减、归零移除
- 状态数值接入结算：力量/敏捷经 `Hook` 修正伤害与格挡，虚弱/易伤参与乘性修正；再生在回合边界经 `Hook.Settle` → `BA_Heal` 结算（敌我共用）
- 卡牌升级全链路：`UD_*` 配置 → `CardModelBaker` 解析为 `CM_ValuePatch` → `CM_Runtime.Upgrade()` 现改数值 / 追加词条 / 移除词条；升级预览用 `ToMutable().Upgrade()` 副本，不写回域
- 遗物系统（跑图域 `GameRun`）：`SO_Relic` + **52 个具体遗物资产**；`RE_*` 触发效果经订阅战斗事件 + 组合 `BattleAction` 接入（零反向依赖），`LizardTail` 经 `IDeathGuard` 介入致命伤；`RelicBaker` 按资产类型分派（漏登记显式 `throw`）。战斗内已可触发
- 药水系统（战斗内可消耗）：`SO_Potion` + 15 个具体药水资产；饮用经 `BC_UsePotion` → `PotionResolver` → `BA_ConsumePotion` 取走并广播 `BE_PotionConsumed`，落点规则全在 Resolver；玩家运行时持 3 槽药水。丢弃走即时旁路
- **战后奖励结算**：`SO_Encounter.Reward` → `EncounterRewardRoller`（同种子可复现）掷出金币 / 卡牌三选一 / 概率药水 / 精英遗物清单 → `UI_RewardPanel` 展示并兑现
- **整局层与多场景阶段**：`RunState` 持有整局进度；`SegmentSceneManager` 按场景名叠加编排 Ancient / Rest / Battle 三阶段；四个 `*LifetimeScope` 各自隔离装配

**表现层（已成型）**

- Spine 动画演出：攻击 / 受击 / 死亡 / 施法，播到 50% 即放行事件链
- 手牌区：弧形排布、悬停偏移+放大+两侧避让、拖拽阻尼跟随、瞄准定住
- 拖拽瞄准：目标箭头沿带弧曲线排布、命中整支变红；落点规则与结算同源
- 牌堆：三堆计数逐张步进 + 进堆弹跳、点击弹出查看面板
- 弹层与特效：伤害/回血数字飘字、受击与回复一次性特效、说明气泡、回合横幅
- 状态特效：状态施加爆发演出 + 常驻状态图标（池化），复用既有事件驱动/池化管线
- 卡面：内嵌牌面按类型切换、稀有度换边框、不可打整卡压暗、数值修正着色、关键词悬停说明
- **先古之民段 / 休息段表现**：对话/选项演出、铁匠（UI_SmithPanel）与休息特效（UI_RestVFXRoot）、整局顶栏（卡组/地图/菜单/遗物/药水栏）

**已有内容资产**

- 卡牌 8 张（Attack / Bash / Block / ForgottenRitual / PommelStrike / SwordBoomerang / Uppercut / Vicious）
- 敌人 1 种（树枝史莱姆 TwigSlime，四条意图 + 最前槽位替换）+ 玩家 1 套
- 状态 5 种（再生 / 力量 / 敏捷 / 易伤 / 虚弱，均数据驱动）
- 词条 10 种 + 升级补丁 4 种 + 意图 6 种
- 遗物 **52 种**（含 Anchor / BurningBlood / CentennialPuzzle / GremlinHorn / HappyFlower / LizardTail / Orichalcum / Pendulum / Shuriken 及 42 个新增内容遗物，战斗内效果已通）
- 药水 15 种（Block / Blood / Energy / Fire / Strength / Weak / PotionOfBinding 等，战斗内饮用已通）
- 场景 3 个：`Scene_Battle` / `Scene_Rest` / `Scene_MainMenu`（整局层驱动 Ancient/Rest/Battle 叠加段）
- 预制体若干（卡面、意图图标、单位、弹层、特效、面板）

### 近期落地（2026-10 下旬至 10-31）

- **多场景阶段架构落地**：新增先古之民段（Ancient）、休息段（Rest）与整局层（Run）；`RunState` 持有整局进度、`SegmentSceneManager` 按场景名叠加编排、`RunLifetimeScope` 注册常驻件，四个 `*LifetimeScope` 各自隔离装配。常驻靠叠加场景 + 组合根，无 `DontDestroyOnLoad`。
- **遗物内容大扩张**：具体遗物从 9 暴增至 **52**，仍保持"数据驱动 + `RelicBaker` 类型分派 + 订阅战斗事件组合动作"模式，零漂移；`RelicBaker` 末位 `_ => throw` 防静默失效。药水同期扩至 15。
- **战后奖励结算**：`SO_Encounter.Reward` + `EncounterRewardRoller`（同种子可复现）+ `UI_RewardPanel` 打通胜利分支（金币 / 卡牌三选一 / 概率药水 / 精英遗物），奖励兑现后继续开地图。战败分支收段照旧。
- **卡牌升级预览**：`CM_Static`/`CardModel` 新增 `CanUpgrade`/`BaseVersion`/`ToMutable()`/`Upgrade()`，三选一/卡组详情复用临时 `ToMutable().Upgrade()` 副本预览，不写回域。
- **牌面复用组件化**：`UI_ConcreteCardView`/`UI_CardSelect`/`UI_CardOneInThreePanel` 全部复用 `UI_CardFace`，无重复渲染。
- **`EngineUtility` 依赖纯净**：已无 `Utility` 依赖，纯 `UniTask` + `DOTween` 引擎件。
- **H-1 彻底解决**：删除 `GameEvent`/`GameRest`/`GameShop` 三个空骨架 asmdef 及目录，依赖图不再表达多余约束。
- **分层澄清**：`CommonPresentation` 与 `Presentation` 为平级表现层、`Presentation` 依赖 `CommonPresentation` 为故意设计；`CommonPresentation → GameRun` 属正常的表现层向下依赖，`UI_RewardPanel` 经注入 `RunState` 兑现奖励在允许方向上（非越界、非架构缺陷，往期误判的 H-2 不成立）。

### 已知未完成 / 待收口（功能完整性 + 架构，非全部为缺陷）

| 项 | 状态 |
| --- | --- |
| `CommonPresentation → GameRun`（按设计，非缺陷） | `CommonPresentation` 与 `Presentation` 平级表现层，`UI_RewardPanel` 经注入 `RunState` 兑现奖励属受控写入口（不越界）。设计观察：该通用奖励面板因直接耦合 `RunState` 写 API，复用范围限于跑图上下文（合理取舍）。 |
| 战斗结束奖励 | 胜利分支已打通；**战败分支仍未接**（照旧立即收段） |
| 卡牌升级正式入口 | 升级数据 → 烘焙 → 运行时应用全链路已通；但"升级获取途径 / 升级 UI 入口"（事件 / 遗物 / 商店）仍未规划 |
| 商店 / 金币 | `SO_Player._startGold` 已定义，零消费 |
| 遗物（局外持有 / 掉落 / 跨战斗生效） | 战斗内效果已通过 `GameRun` 接通，但遗物的获取 / 持有 / 掉落与跑图进度循环部分已就位（`RunState` 持有）；仍需与地图节点 / 事件闭环 |
| 药水（局外持有 / 掉落 / 获取途径） | 战斗内饮用 / 丢弃 / 效果结算已通；战斗奖励可掉落药水，商店 / 事件等其它获取途径未做 |
| 格挡 / 治疗意图数值 | 数据层有 `Amount`，意图图标均为"纯图标占位"，玩家无法预判敌人会挡多少 / 回多少 |
| 音效 | 代码中已留触发点 TODO（发牌、进堆），音效层尚未开写 |
| `BattleEventDispatcher.Unregister` | 单位视图销毁后事件处理器仍留在字典（单场战斗内有界滞留，靠表现层 null 检查兜住，无实际 bug） |
| `UI_RestVFXRoot` 视图内 `FindObjectOfType` | 小瑕疵，建议注入 `UI_RestBackgroundRoot` |

---

## 后续计划

> 按依赖顺序排列，前项未完成时后项会返工。单人开发，按"先让已有数据生效、再扩系统、同时修架构异味"推进。

### 阶段一：收口当前重构（已完成 ✅）

1. ✅ 手牌区四组件各司其职（编排 / 悬停裁决 / 瞄准 / 演出）。
2. ✅ 通用对象池（`ObjectPool<T>` + `MonoObjectPool<T>`）与可复用表现件抽取。
3. ✅ 状态数值系统闭环（`Hook` + 重写 `ModifiableStat`）。
4. ✅ 卡牌升级子系统全链路打通。
5. ✅ `EngineUtility` / `CommonPresentation` / 三新场景阶段拆出，跨场景复用边界建立。
6. ✅ **删除 `GameEvent`/`GameRest`/`GameShop` 三个空骨架**（H-1 解决）。

### 阶段二：补齐战斗内功能空洞 + 修架构异味（最高优先级）

1. **战斗结束奖励流程**：胜利 / 失败分支、奖励展示与选择、结果落回 `RunState`。
2. `BattleEventDispatcher` 增 `Unregister`，单位视图在 `OnDestroy` 反注册事件处理器。
3. 敌方意图图标补数值展示（与卡面 `CardValueRenderer` 同源）。
4. 升级正式入口：升级 UI + 获取途径（事件 / 遗物 / 商店三选一）。
5. 音效层 `Presentation/Feedback`：订阅 `BE_*` 播发声，不在视图里直调。

### 阶段三：可扩展性加固

1. `PerkBaker` / `CardModelBaker` / `IntentBaker` / `Hook.Settle` 的 `switch` 加 `default` 报错，把"忘加 case"从静默失效变成可见失败（`RelicBaker`/`EncounterRewardRoller` 已先行）。
2. ~~`BattleTargetDirector` 抽 `IAimClient` 接口~~ —— **已完成**（M-1 已解决）。
3. 清理未使用依赖（`Presentation → Unity.Mathematics` 等死引用）；删除死代码 `EngineUtility/UI/UI_SingletonObject`（`DontDestroyOnLoad` 助手，零引用）。
4. 阵型上限（`MaxEnemyCount`）收进 `BattleSlots`，与槽位定义同源。
5. `UI_CardBattle` 再切一刀：姿态通道与补间通道合并为 `CardPose`。
6. 分派守卫为手抄惯用法，建议抽一层微型 `ReentrancyGuard`/`RunOnceGuard` 并区分 `IDeathGuard` 命名（M-6）。
7. `GameRun` 轻微偏离载荷纪律（`RelicEffect` 常驻 SO 引用、`RelicTriggerNotifier` 以 SO 作事件载荷），新增时优先回 struct 载荷（M-8）。

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
3. 打开 `Assets/Scenes/Scene_MainMenu.unity`（整局层入口，会叠加编排 Ancient/Rest/Battle 段）运行

> 注：当前 3 个场景（`Scene_MainMenu` / `Scene_Rest` / `Scene_Battle`）由 `SegmentSceneManager` 按场景名叠加加载；直接打开 `Scene_Battle` 也能独立运行战斗段（依赖注入由 `BattleLifetimeScope` 接管）。

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
