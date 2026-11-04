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

一句话总纲：**八个程序集、一张无环依赖 DAG、四通道解耦（命令上行 / 事件下行 / 只读查询 / 状态介入）；配置只读一次即烘焙为运行时对象，规则与状态只存在一处；表现层与战斗逻辑经四通道解耦，跨战斗段的写入口经显式注入的 `RunState` 引用完成（不绕过动作系统）；六个场景阶段（先古之民段 / 休息段 / 战斗段 / 宝箱段 / 商店段 / 事件段）由整局层（RunState）统一持有进度、按场景名叠加编排。**

> 详细逐文件评审见 [Doc/架构分析报告.md](Doc/架构分析报告.md)（综合评分 **94 / 100（A）**）。

### 程序集与依赖（asmdef 强制，已用 GUID 反解核验无环）

```
【叶层 / 域层：仅依赖外部包或下层域】
  Utility        （纯 C#，零 UnityEngine / 零项目内依赖）
  ConfigData     （纯资产，零执行逻辑；外部：NaughtyAttributes, SerializeReferenceExtensions）
  RuntimeData    ──→ {Utility, ConfigData}
  EngineUtility  ──→ {UniTask, DOTween}          （已无 Utility 依赖，纯引擎件）
  GameBattle     ──→ {Utility, RuntimeData, ConfigData} + UniTask
  GameRun        ──→ {GameBattle, RuntimeData, ConfigData, Utility} + UniTask   ✅ 已实现（遗物 ~57 / 奖励 / 整局 RunState）

【表现层（平级两层；Presentation 依赖 CommonPresentation 为故意设计）】
  CommonPresentation ──→ {Utility, ConfigData, RuntimeData, EngineUtility, GameBattle, GameRun} + 外部包（VContainer/UniTask/TMP/NaughtyAttributes/DOTween）
  Presentation        ──→ {CommonPresentation + 上述全部域层} + 外部包（VContainer/UniTask/TMP/InputSystem/Spine/NaughtyAttributes/DOTween）
```

> 注：`CommonPresentation` 与 `Presentation` 为**平级的表现层**——通用表现件集中在 `CommonPresentation`，各场景阶段专属表现放 `Presentation`；`Presentation` 依赖 `CommonPresentation` 是故意设计。`CommonPresentation → GameRun` 是表现层对域层的正常向下依赖，`UI_RewardPanel` 经注入的 `RunState` 兑现奖励在允许方向上，**非架构缺陷**（往期曾被误判为 H-2"越界写耦合"，经澄清不成立）。

`ConfigData` 是纯资产/数据层，不依赖 `Utility`——实际是扇形汇入而非单链。全部程序集构成有向无环图，编译期即生效，无任何反向引用或环。

> **关于空骨架程序集**：`GameEvent` / `GameRest` / `GameShop` 三个空骨架 asmdef **已于 2026-10-31 删除**（H-1 彻底解决）。当前 8 个 asmdef 全部有实质代码。

### 八个有实质代码的域

| 程序集                    | 职责                        | 关键内容                                                                                                  |
| ---------------------- | ------------------------- | ----------------------------------------------------------------------------------------------------- |
| **Utility**            | 纯 C# 通用件（零业务、零引擎依赖）       | 状态机 `StateMachine<T>`、确定性随机 `Rng`、可修正数值引擎 `ModifiableStat`、纯 C# 对象池 `ObjectPool<T>`                  |
| **EngineUtility**      | 引擎级通用件（依赖 UnityEngine，不含业务） | 组件池 `MonoObjectPool<T>`、贝塞尔 `Curve`、UI 几何 `UiGeometry`、补间 `UiTween`、悬停执行器、按钮/面板抽象基类（已无 `Utility` 依赖）、`UI_RowScaleFitter`（行缩放适配） |
| **ConfigData**         | 配置资产（策划可编辑，只有"做什么、做多少"）   | `SO_Player` / `SO_Enemy` / `SO_Card`、状态资产、`AD_*` 词条 / `UD_*` 升级补丁 / 意图数据、**遗物 `SO_Relic` + ~60 具体 `SO_*` + 2 池**、**药水 `SO_Potion` + 17 具体 `SO_*`**、**休息选项 `SO_RestOption` + 4 具体（Cook/Dig/Forge/Heal）**、**事件系统 `SO_Event` + `EEventCondition` + `EventOptionData` + 9 个 `EED_*` 效果数据 + 5 个具体事件**、**商店 `SO_ShopPrices`**、Ancient 5 文件、Run/Encounter 6 文件、切肉刀遗物 `SO_MeatCleaver`、6 个新遗物 Concrete |
| **RuntimeData**        | 运行时对象（纯 C#，配置只读一次即烘焙）    | `Creature` / `Player`（3 槽药水 + 持卡组/遗物引用）/ `CardModel`(=`CM_Static`+`CM_Runtime`+`CM_ValuePatch`) / `CardPile`、`BattlePerk`+`BP_*`、`CardAffix`+`CA_*`、三个烘焙器、`EI_*` 意图 |
| **GameBattle**         | 战斗域（流程、规则、桥）              | 八阶段状态机、20 个 `BattleAction` + `ActionSystem`、状态介入层 `Hook`、三个 Resolver、命令/事件/查询三桥、分派守卫、奖励注入接线          |
| **GameRun**            | 跑图/局外域（**已大幅落地**）           | `RelicEffect`（订阅战斗事件+组合动作）+ `RelicBaker`（按 `SO_Relic` 子类分派并登记 `IDeathGuard`）+ 57 个 `RE_*`；`Reward`（`ERewardKind`/`RunReward`/`EncounterRewardRoller`）；`Run`（`RunState` 整局进度 + 受控写入口 + 事件下行） |
| **CommonPresentation** | 通用表现层（与 `Presentation` **平级**，被其依赖） | `UI_CardFace` + `UI_CardFaceSelector` + `UI_ConcreteCardView` + `UI_CardSelect` + `CardValueRenderer` + `CardSorter`、`UI_HoverTip` + `PopFactory`、`UI_NumberPopTip`、VFX 族（含 `UI_SmithForgeVfx`）、`UI_RewardPanel`（经注入 `RunState` 兑现奖励，表现层→域的正常方向）、**跨段弹板基类 `UI_CardSelectPanel` + `UI_CardRemovePanel` / `UI_CardUpgradePanel`（已迁至 `CardOperationPanel`）** |
| **Presentation**       | 各场景阶段专属表现与装配（**六阶段 + 整局层**） | Battle（单位/卡牌/HUD/瞄准/弹层特效/奖励泊入）；**Ancient 段**；**Rest 段**（含休息选项 UI）；**Chest 段（宝箱段）**；**Shop 段（商店段，本轮新增）**；**Event 段（事件段，本轮新增，此前 L-9 零引用死代码已接入）**；**Run 整局层**（TopBar + `SegmentSceneManager` 阶段编排）；DI 组合根 `Battle/Ancient/Rest/Chest/Shop/Event/Run` 七个 `*LifetimeScope` |

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

**通道 ③ 与 ④ 共用同一份 `Hook.Resolve`**，因此"卡面预览的伤害"与"实际扣掉的血"在结构上不可能不一致。六大场景阶段的表现层均严格走命令/事件/查询上行下行，未引入平行机制（非战斗段写状态当前直接改写 `RunState`/玩家数值，见「已知未完成」HK-1）。

### 多场景阶段与整局层

- **整局层 `RunState`**（`GameRun/Run`，纯 C#）：持有 `Floor`/`Rng`/`Player` 的遗物·药水·卡组等整局状态；数值变更以 `event Action` 下行通知 HUD。
- **阶段编排 `SegmentSceneManager`**（`Presentation/_Bootstrap/Run`，250 行）：按**场景名字符串**（`Scene_Ancient`/`Scene_Rest`/`Scene_Battle`/`Scene_Chest`/`Scene_Shop`/`Scene_Event`）Additive 加载/卸载阶段，无对下一阶段类型的硬引用；整局常驻靠 `RunLifetimeScope` + 叠加场景实现，**无 `DontDestroyOnLoad`**。
- **七个 `*LifetimeScope`**（Ancient/Rest/Chest/Shop/Event/Run + Battle）：结构一致，`Configure` 内只装配本阶段类型，各段 scope 互不引用。商店段与事件段均以与既有阶段完全一致的 `LifetimeScope + IStartable` 模板零漂移落地。

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

### 代码规模（2026-11-03 工作树，`HEAD = 8a1c929`）

| 域                   |      文件 |       行数 |
| ------------------- | ------: | -------: |
| Utility             |       7 |      237 |
| ConfigData          |     159 |     2551 |
| RuntimeData         |      42 |     1689 |
| EngineUtility       |      15 |      865 |
| GameBattle          |      77 |     2479 |
| GameRun             |      58 |     2108 |
| CommonPresentation  |      31 |     3292 |
| Presentation        |     101 |     9416 |
| **合计**              | **490** | **22637** |

464 / 490 个文件在 150 行以内（94.7%）。超出的 26 个均为内聚单体（按域：Presentation 18 / CommonPresentation 6 / RuntimeData 2）：`UI_HandView` 452 / `UI_PotionGroup` 400 / `UI_ShopRugPanel` 378 / `UI_CardBattle` 366 / `UI_CardSelectPanel` 334 / `ShopSession` 277 / `UI_CardFace` 262 / `EventSession` 261 / `SegmentSceneManager` 250 / `UI_HandHover` 227 / `UI_HandShow` 215 / `UI_RelicSlot` 210 / `UI_HoverTip` 210 / `RestEntry` 208 / `UI_SmithForgeVfx` 202 / `UI_Actor` 198 / `UI_RewardPanel` 192 / `AncientEntry` 184 / `UI_TopBar` 181 / `BattleTargetDirector` 178 / `UI_PileCardsView` 164 / `UI_DeckPanel` 163 / `UI_RemoveCardButton` 162 / `CardModelBaker` 160 / `CM_Runtime` 156 / `UI_CardUpgradePanel` 156。

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
- 遗物系统（跑图域 `GameRun`）：`SO_Relic` + **~57 个具体遗物资产**；`RE_*` 触发效果经订阅战斗事件 + 组合 `BattleAction` 接入（零反向依赖），`LizardTail` 经 `IDeathGuard` 介入致命伤；`RelicBaker` 按资产类型分派（漏登记显式 `throw`）。战斗内已可触发
- 药水系统（战斗内可消耗）：`SO_Potion` + 17 个具体药水资产；饮用经 `BC_UsePotion` → `PotionResolver` → `BA_ConsumePotion` 取走并广播 `BE_PotionConsumed`，落点规则全在 Resolver；玩家运行时持 3 槽药水。丢弃走即时旁路
- **战后奖励结算**：`SO_Encounter.Reward` → `EncounterRewardRoller`（同种子可复现）掷出金币 / 卡牌三选一 / 概率药水 / 精英遗物清单 → `UI_RewardPanel` 展示并兑现
- **整局层与多场景阶段**：`RunState` 持有整局进度；`SegmentSceneManager` 按场景名叠加编排 Ancient / Rest / **Chest** / Battle / **Shop** / **Event** 六阶段；七个 `*LifetimeScope`（Ancient/Rest/Chest/Shop/Event/Run + Battle）各自隔离装配
- **宝箱段（第四场景阶段）**：`ChestLifetimeScope`（`Parent.Resolve<RunState>()` + 普通/稀有 `SO_RelicPool` 装配 `ChestSession`）+ `ChestEntry`（`IStartable` 驱动开箱→出遗物→领取→闪光锁定→收段）+ `UI_Chest`（Spine 骨骼 + 指针交互）+ `UI_ChestProceedButton`（`UI_Button` 子类，`[Inject] SegmentSceneManager` 收段）；与既有阶段完全同模板、零结构漂移
- **商店段（第五场景阶段，本轮新增）**：`ShopLifetimeScope`（`Parent.Resolve<RunState>()` + `SO_ShopPrices` 装配 `ShopSession`）+ `ShopEntry`（`IStartable` 进店演出→货架陈列→购买交互→收段）+ `UI_ShopMerchant`/`UI_MerchantHand`/`UI_ShopRugPanel`/`UI_ShopItem`(基类)/`UI_CardShop`/`UI_PotionShopItem`/`UI_RelicShopItem`/`UI_RemoveCardButton`/`UI_ShopProceedItem`；与既有阶段完全同模板、零结构漂移；购买经 `_session.TryBuyX` 改写 `RunState`，UI 全部 `[Inject] RunState` 只读
- **事件段（第六场景阶段，本轮新增）**：`EventLifetimeScope` + `EventEntry`(`IStartable`) + `EventSession`（持 UI 引用 + UniTask，选项点击→`WaitChoiceAsync`→执行 `EED_*` 效果）；配套**事件配置数据驱动系统**（见下）；此前 L-9 零引用死代码已随本段接入而关闭
- **跨段复用弹板基类合并（本轮）**：抽象基类 `UI_CardSelectPanel`（继承 `UI_Panel`，沉淀选卡/多选/确认/特写/演出/结果交付）+ `UI_CardRemovePanel`（删牌）/ `UI_CardUpgradePanel`（锻造升级）继承并实现 `CollectCandidates`/`OnConfirm`/`PlayVfx` 钩子，复用 `UI_ConcreteCardView`/`VfxFactory`/`MonoObjectPool`；旧目录已删，去重约 390 行

**事件配置数据驱动系统（本轮新增）**

- `SO_Event`（事件主配置，含 `List<EventOptionData>`）+ `EEventCondition`（选项可用性枚举 + 条件值）+ `EventOptionData`（选项：描述 + 条件 + `List<EventEffectData>` 顺序效果）
- `EventEffectData` 抽象基类 + **9 个 `EED_*` 效果数据**（`AddCard`/`GainGold`/`LoseHp`/`RandomRelic`/`RelicChoice`/`RemoveCards`/`RestoreHp`/`RewardPotions`/`SpendGold`），均以 `[SerializeReference]`+`[SubclassSelector]` 实现效果多态——新增效果类型零改既有逻辑
- 5 个具体事件（`SO_DollRoom`/`SO_DoorsOfLightAndDark`/`SO_GraveOfTheForgotten`/`SO_ThisOrThat`/`SO_Wellspring`）为纯数据实例化、零逻辑

**休息选项数据驱动系统（本轮新增）**

- `SO_RestOption` 抽象基类 + 4 个具体选项（`SO_RestOption_Cook`/`_Dig`/`_Forge`/`_Heal`，纯数据），行为在 `RestEntry` 段内分派；同构于遗物"数据 + 段入口分派"范式

**表现层（已成型）**

- Spine 动画演出：攻击 / 受击 / 死亡 / 施法，播到 50% 即放行事件链
- 手牌区：弧形排布、悬停偏移+放大+两侧避让、拖拽阻尼跟随、瞄准定住
- 拖拽瞄准：目标箭头沿带弧曲线排布、命中整支变红；落点规则与结算同源
- 牌堆：三堆计数逐张步进 + 进堆弹跳、点击弹出查看面板
- 弹层与特效：伤害/回血数字飘字、受击与回复一次性特效、说明气泡、回合横幅
- 状态特效：状态施加爆发演出 + 常驻状态图标（池化），复用既有事件驱动/池化管线
- 卡面：内嵌牌面按类型切换、稀有度换边框、不可打整卡压暗、数值修正着色、关键词悬停说明
- **先古之民段 / 休息段 / 宝箱段 / 商店段 / 事件段表现**：对话/选项演出、铁匠（UI_SmithPanel）与休息特效（UI_RestVFXRoot，往期视图内 `FindObjectOfType`/`Update` 异味已随目录重排移除）、宝箱（UI_Chest Spine 骨骼 + 指针交互）、商店（UI_ShopMerchant 货架 + UI_ShopRugPanel 三套货架刷新）、事件（UI_EventPanel 选项组）、整局顶栏（卡组/地图/菜单/遗物/药水栏）

**已有内容资产**

- 卡牌 8 张（Attack / Bash / Block / ForgottenRitual / PommelStrike / SwordBoomerang / Uppercut / Vicious）
- 敌人 1 种（树枝史莱姆 TwigSlime，四条意图 + 最前槽位替换）+ 玩家 1 套
- 状态 5 种（再生 / 力量 / 敏捷 / 易伤 / 虚弱，均数据驱动）
- 词条 10 种 + 升级补丁 4 种 + 意图 6 种
- 遗物 **~57 种**（含 Anchor / BurningBlood / CentennialPuzzle / GremlinHorn / HappyFlower / LizardTail / MeatCleaver / Orichalcum / Pendulum / Shuriken 及 40+ 个新增内容遗物 + 6 个一次性遗物 `SO_ArcaneScroll`/`GoldenPearl`/`NutritiousOyster`/`Pomander`/`PrecariousShears`/`SmallCapsule`，战斗内/休息/宝箱/商店效果已通；`SO_MeatCleaver` 经 `RelicBaker` 显式 `=> null` 登记、效果走休息烹饪流）
- 药水 17 种（Block / Blood / Energy / Fire / Strength / Weak / PotionOfBinding 等，战斗内饮用已通）
- 事件 5 种（DollRoom / DoorsOfLightAndDark / GraveOfTheForgotten / ThisOrThat / Wellspring，纯数据配置）
- 休息选项 4 种（Cook / Dig / Forge / Heal）
- 场景 6 个阶段：`Scene_Battle` / `Scene_Rest` / `Scene_Chest` / `Scene_Shop` / `Scene_Event` / `Scene_MainMenu`（整局层驱动 Ancient/Rest/Chest/Battle/Shop/Event 叠加段）
- 预制体若干（卡面、意图图标、单位、弹层、特效、面板）

### 近期落地（2026-10 下旬至 11-03）

- **商店段（第五场景阶段）上线**：新增 `ShopLifetimeScope`（`Parent.Resolve<RunState>()` + `SO_ShopPrices` 装配 `ShopSession`）、`ShopEntry`（`IStartable` 进店演出→货架陈列→购买交互→收段）、`UI_ShopMerchant`/`UI_MerchantHand`/`UI_ShopRugPanel`/`UI_ShopItem`/`UI_CardShop`/`UI_PotionShopItem`/`UI_RelicShopItem`/`UI_RemoveCardButton`/`UI_ShopProceedItem`/`EShopBuyResult`。与既有阶段完全同模板、零结构漂移，是第五阶段以零成本加入的正面证据。
- **事件段（第六场景阶段）上线 + 事件配置数据驱动系统**：新增 `EventLifetimeScope`/`EventEntry`/`EventSession` 与事件配置系统（`SO_Event` + `EEventCondition` + `EventOptionData` + 9 个 `EED_*` 效果数据多态 + 5 个纯数据事件）；此前 L-9 的 `UI_EventPanel`/`UI_EventOptionItem`/`UI_EventProceedItem` 零引用死代码已随本段接入而关闭。
- **休息选项数据驱动**：`SO_RestOption` + 4 个具体选项（Cook/Dig/Forge/Heal），行为在 `RestEntry` 段内分派。
- **跨段弹板基类合并**：抽象 `UI_CardSelectPanel`（继承 `UI_Panel`）+ `UI_CardRemovePanel`/`UI_CardUpgradePanel`（继承并实现钩子），复用 `UI_ConcreteCardView`/`VfxFactory`/`MonoObjectPool`；旧目录已删，去重约 390 行。
- **内容资产续增**：具体遗物由 ~54 增至 **~57**（新增 6 个一次性遗物，均经 `RelicBaker` 显式 `=> null` 登记 + 末位 `_ => throw` 兜底、无静默缺失）；药水维持 17；新增 5 事件 / 4 休息选项 / 商店价格配置，均保持"数据驱动"模式、零漂移。
- **多场景阶段架构落地（往期）**：新增先古之民段（Ancient）、休息段（Rest）与整局层（Run）；`RunState` 持有整局进度、`SegmentSceneManager` 按场景名叠加编排、`RunLifetimeScope` 注册常驻件，各 `*LifetimeScope` 各自隔离装配。
- **H-1 彻底解决（往期）**：删除 `GameEvent`/`GameRest`/`GameShop` 三个空骨架 asmdef 及目录。
- **分层澄清（往期）**：`CommonPresentation` 与 `Presentation` 为平级表现层、`Presentation` 依赖 `CommonPresentation` 为故意设计；`CommonPresentation → GameRun` 属正常的表现层向下依赖，`UI_RewardPanel` 经注入 `RunState` 兑现奖励在允许方向上（非越界、非架构缺陷，往期误判的 H-2 不成立）。

### 已知未完成 / 待收口（功能完整性 + 架构，非全部为缺陷）

| 项 | 状态 |
| --- | --- |
| **HK-1｜商店/事件段规则写在 `Presentation` 域** | `ShopSession`/`EventSession` 直接改写 `_run.Gold -=`（绕过 `RunState.GainGold` 受控入口）、HP 直写 `Creature.CurrentHp` 不触发变更事件（HUD 不同步隐患）。建议将"段规则"下沉至 `GameRun` 域模型，补 `RunState.SpendGold` 受控入口；爆破半径仅 2 文件，修复成本低。**属本轮新增观察，扣分项** |
| `CommonPresentation → GameRun`（按设计，非缺陷） | `CommonPresentation` 与 `Presentation` 平级表现层，`UI_RewardPanel` 经注入 `RunState` 兑现奖励属受控写入口（不越界）。设计观察：该通用奖励面板因直接耦合 `RunState` 写 API，复用范围限于跑图上下文（合理取舍） |
| 战斗结束奖励 | 胜利分支已打通；**战败分支仍未接**（照旧立即收段） |
| 卡牌升级正式入口 | 升级数据 → 烘焙 → 运行时应用全链路已通；但"升级获取途径 / 升级 UI 入口"（事件 / 遗物 / 商店）仍未规划 |
| 商店 / 金币 | `SO_Player._startGold` 已定义；商店消费端已就位（金币字段、牌堆视图可复用），金币循环闭环待与事件/地图打通 |
| 遗物（局外持有 / 掉落 / 跨战斗生效） | 战斗内效果已通过 `GameRun` 接通，`RunState` 持有已就位；仍需与地图节点 / 事件闭环 |
| 药水（局外持有 / 掉落 / 获取途径） | 战斗内饮用 / 丢弃 / 效果结算已通；战斗奖励可掉落药水，商店 / 事件等其它获取途径未做 |
| 格挡 / 治疗意图数值 | 数据层有 `Amount`，意图图标均为"纯图标占位"，玩家无法预判敌人会挡多少 / 回多少 |
| 音效 | 代码中已留触发点 TODO（发牌、进堆），音效层尚未开写 |
| `BattleEventDispatcher.Unregister` | 单位视图销毁后事件处理器仍留在字典（单场战斗内有界滞留，靠表现层 null 检查兜住，无实际 bug） |
| `UI_SingletonObject` 死代码 | `EngineUtility/UI/UI_SingletonObject`（`DontDestroyOnLoad` 助手，零引用），建议删除 |
| `UI_OneShotVfx` 魔法字符串 | `_clip.name` 当 Animator 状态名（L-1，往期已标未修），动画状态改名即静默失效 |
| ~~`Presentation/Event` 三构件零引用~~ | **已解决**：事件段接入后 `UI_EventPanel`/`UI_EventOptionItem`/`UI_EventProceedItem` 已实际引用，死代码问题消除 |
| ~~`UI_RestVFXRoot` 视图内 `FindObjectOfType`~~ | **已解决**：目录重排后该文件不再含查找/轮询 |

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
7. ✅ **宝箱段（第四场景阶段）上线** + 跨段复用删牌/锻造弹板 + 统一卡牌详情特写与弹层停泊。
8. ✅ **休息烹饪 + 切肉刀遗物**；目录重排后 `UI_RestVFXRoot` 视图查找异味彻底移除。
9. ✅ **商店段（第五阶段）+ 事件段（第六阶段）上线** + 事件/休息配置数据驱动系统 + 跨段弹板基类合并（去重约 390 行）。

### 阶段二：补齐战斗内功能空洞 + 修架构异味（最高优先级）

1. **HK-1 收口**：将 `ShopSession`/`EventSession` 的"段规则"（金币扣减/HP/卡/遗物结算）下沉至 `GameRun` 域模型，表现层仅做异步编排与调用；补 `RunState.SpendGold` 受控扣款入口，HP 写入经带通知属性。
2. **战斗结束奖励流程**：胜利 / 失败分支、奖励展示与选择、结果落回 `RunState`。
3. `BattleEventDispatcher` 增 `Unregister`，单位视图在 `OnDestroy` 反注册事件处理器。
4. 敌方意图图标补数值展示（与卡面 `CardValueRenderer` 同源）。
5. 升级正式入口：升级 UI + 获取途径（事件 / 遗物 / 商店三选一）。
6. 音效层 `Presentation/Feedback`：订阅 `BE_*` 播发声，不在视图里直调。

### 阶段三：可扩展性加固

1. `PerkBaker` / `CardModelBaker` / `IntentBaker` / `Hook.Settle` 的 `switch` 加 `default` 报错，把"忘加 case"从静默失效变成可见失败（`RelicBaker`/`EncounterRewardRoller` 已先行）。
2. ~~`BattleTargetDirector` 抽 `IAimClient` 接口~~ —— **已完成**（M-1 已解决）。
3. 清理未使用依赖（`Presentation → Unity.Mathematics` 等死引用）；删除死代码 `EngineUtility/UI/UI_SingletonObject`（`DontDestroyOnLoad` 助手，零引用）；修 `UI_OneShotVfx` 的 `_clip.name` 魔法字符串（L-1）。
4. 阵型上限（`MaxEnemyCount`）收进 `BattleSlots`，与槽位定义同源。
5. `UI_ShopRugPanel` 抽泛型货架刷新辅助，降重三套高度雷同的 Bind/Refresh 样板（M-5）。
6. 分派守卫为手抄惯用法，建议抽一层微型 `ReentrancyGuard`/`RunOnceGuard` 并区分 `IDeathGuard` 命名（M-6）。
7. `GameRun` 轻微偏离载荷纪律（`RelicEffect` 常驻 SO 引用、`RelicTriggerNotifier` 以 SO 作事件载荷），新增时优先回 struct 载荷（M-8）。
8. `SO_ShopPrices` 的报价计算逻辑下沉到商店段模型，SO 仅留字段（M-9）。

### 阶段四：新系统（视毕设进度决定是否开写）

1. **局内地图与楼层编排**：在 `GameRun` 程序集内实现，方向 `GameRun → GameBattle`（战斗不认识跑图）；`EventSession.PickEvent` 注释已预留地图分层接入点。
2. **遗物系统（局外闭环）**：战斗内效果已通（`GameRun`），下一步做遗物的获取 / 持有 / 掉落与跑图进度循环，作为局内成长的主承载。
3. **商店与金币闭环**：消费端已就位（金币字段、牌堆视图可复用），打通获取与掉落后形成经济循环。

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
3. 打开 `Assets/Scenes/Scene_MainMenu.unity`（整局层入口，会叠加编排 Ancient/Rest/Chest/Battle/Shop/Event 段）运行

> 注：当前 6 个场景阶段（`Scene_MainMenu` / `Scene_Rest` / `Scene_Chest` / `Scene_Shop` / `Scene_Event` / `Scene_Battle`）由 `SegmentSceneManager` 按场景名叠加加载；直接打开 `Scene_Battle` 也能独立运行战斗段（依赖注入由 `BattleLifetimeScope` 接管）。

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
