# DreamKeep · 梦境守望

沉睡者的意识世界。你是**守梦人**——沉眠中唯一清醒的自我，梦魇正沿梦的走廊向觉醒边界压境。

---

## 环境要求

| 项       | 值                                      |
| ------- | -------------------------------------- |
| Unity   | `2022.3.62f3c1`（LTS 国内版 c1）            |
| 渲染管线    | Built-in                               |
| Git LFS | **必需**（美术 / 音频 / 动画资源走 LFS，未安装会导致签出失败） |
| IDE     | JetBrains Rider 或 Visual Studio 2022   |

> IDE 只做 IntelliSense / 重构 / 调试。**编译权永远归 Unity**——不要在 IDE 里 Build Solution 或 NuTask Restore。用 File > Open 打开项目根目录，不要打开 `.sln`。

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

一句话总纲：**八个程序集、一张无环依赖 DAG、双层四通道解耦（战斗级 + 场景段级）；配置只读一次即烘焙为运行时对象，规则与状态只存在一处；六个场景阶段（先古之民 / 休息 / 战斗 / 宝箱 / 商店 / 事件）的运行时逻辑全部归位 `GameRun` 域，每段配 `PresentationBridge`（事实下行 + 命令门面上行），表现层只做编排、不持业务规则；整局进度由 `RunState` 持有，地图节点（`StandardMapGenerator`）驱动阶段叠加编排。**

> 详细逐文件评审见 [Doc/架构分析报告.md](Doc/架构分析报告.md)（综合评分 **96 / 100（A）**）。

### 程序集与依赖（asmdef 强制，已用 GUID 反解核验无环）

```
【叶层 / 域层：仅依赖外部包或下层域】
  Utility        （纯 C#，零 UnityEngine / 零项目内依赖）
  ConfigData     （纯资产，零执行逻辑；外部：NaughtyAttributes, SerializeReferenceExtensions）
  RuntimeData    ──→ {Utility, ConfigData}
  EngineUtility  ──→ {UniTask, DOTween}          （已无 Utility 依赖，纯引擎件）
  GameBattle     ──→ {Utility, RuntimeData, ConfigData} + UniTask
  GameRun        ──→ {GameBattle, RuntimeData, ConfigData, Utility} + UniTask   ✅ 整局领域层（段域化核心：六段 Loop/Session + RunState + RunFlow + 地图）

【表现层（平级两层；Presentation 依赖 CommonPresentation 为故意设计）】
  CommonPresentation ──→ {Utility, ConfigData, RuntimeData, EngineUtility, GameBattle, GameRun} + 外部包
  Presentation        ──→ {CommonPresentation + 上述全部域层} + 外部包
```

> 注：`CommonPresentation` 与 `Presentation` 为**平级的表现层**——通用表现件集中在 `CommonPresentation`，各场景阶段专属表现放 `Presentation`；`Presentation` 依赖 `CommonPresentation` 是故意设计。`CommonPresentation → GameRun` 是表现层对域层的正常向下依赖。全部程序集构成有向无环图，编译期即生效，无任何反向引用或环。

### 八个有实质代码的域

| 程序集                    | 职责                        | 关键内容                                                                                                  |
| ---------------------- | ------------------------- | ----------------------------------------------------------------------------------------------------- |
| **Utility**            | 纯 C# 通用件（零业务、零引擎依赖）       | 状态机 `StateMachine<T>`、确定性随机 `Rng`、可修正数值引擎 `ModifiableStat`、纯 C# 对象池 `ObjectPool<T>`                  |
| **EngineUtility**      | 引擎级通用件（依赖 UnityEngine，不含业务） | 组件池 `MonoObjectPool<T>`、贝塞尔 `Curve`、UI 几何 `UiGeometry`、补间 `UiTween`、悬停执行器、按钮/面板抽象基类、`UI_SingletonObject`（零引用死代码） |
| **ConfigData**         | 配置资产（策划可编辑，只有"做什么、做多少"）   | `SO_Player` / `SO_Enemy` / `SO_Card`、状态资产、`AD_*` 词条 / `UD_*` 升级补丁 / 意图数据、**遗物 `SO_Relic` ~60 具体 `SO_*` + 2 池**、**药水 `SO_Potion` ~15 具体**、**休息选项 `SO_RestOption` + 4 具体**、**事件系统 `SO_Event` + `EED_*` 效果数据 ~10 + 5 具体事件**、**商店 `SO_ShopPrices`**、地图配置 `SO_Map`/`MapConfig` |
| **RuntimeData**        | 运行时对象（纯 C#，配置只读一次即烘焙）    | `Creature`（HP/MaxHp，setter 抛 `HpChanged`/`Healed`）/ `Player`（3 槽药水 + 持卡组/遗物引用）/ `CardModel`(`CM_Runtime`+`CM_ValuePatch`) / `CardPile`、`BattlePerk`+`BP_*`、`CardAffix`+`CA_*`、烘焙器、`EI_*` 意图 |
| **GameBattle**         | 战斗域（流程、规则、桥）              | 八阶段状态机、`BattleAction`(`BA_*`)+`ActionSystem`、状态介入层 `Hook`、三个 Resolver、命令/事件/查询四桥、**「句柄拆分 + 收尾裁决下沉」**（胜负裁定移入域 `BattleContext`+`BP_BattleFinish`）、分派守卫 |
| **GameRun**            | 跑图/局外域（**整局领域层，已段域化**）     | `RunState`（受控写入口 `GainGold`/`SpendGold`/`RestorePlayerHp`/`AddCard`/`AddRelic`/`TryAddPotion`/`EnterMapNode`/`CompleteMapNode`）+ `RunFlow`（节点→段映射）+ **六段 Loop/Session**（`AncientLoop`/`RestLoop`/`ShopSession`/`EventSession`/`ChestSession`/`BattleSession`，每段配 `PresentationBridge` 事实/命令门面）+ **地图系统**（`StandardMapGenerator` + `MapGraph`/`MapNode`）+ 遗物 `RE_*`(51) + `RelicBaker`(`_ => throw` 兜底) |
| **CommonPresentation** | 通用表现层（与 `Presentation` **平级**，被其依赖） | `UI_CardFace`/`UI_ConcreteCardView`/`UI_CardSelectPanel`、`UI_HoverTip`+`PopFactory`、`UI_NumberPopTip`、VFX 族（`UI_SmithForgeVfx`）、`UI_RewardPanel`（兑现奖励，见「已知未完成」K1） |
| **Presentation**       | 各场景阶段专属表现与装配（**六阶段 + 整局层 + 地图**） | Battle（单位/卡牌/HUD/瞄准/弹层特效）；Ancient/Rest/Chest/Shop/Event 五段 UI（均经 `XxxCommandDispatcher` 上发、订阅 `SegmentEventDispatcher` 事实）；Run 整局层（`UI_TopBar` + `UI_MapView` 地图 + `SegmentSceneManager`）；7 个 `*LifetimeScope` 隔离装配 |

### 双层四通道通信

**① 战斗级（`GameBattle` 内）**

```
① 上行·命令：视图 → BattleCommandDispatcher → BattleLoop.EnqueueCommand(new BC_*，纯数据) → 等待态阶段取出执行
② 下行·事件：BattleAction.Execute → List<IBattleEvent>(BE_* struct 只读) → BattleEventDispatcher.PlayAsync → 视图并行演出
③ 上行·只读查询：表现层 → BattleQueryDispatcher.PreviewXxx → 直算返回（与结算共用 Hook.Resolve）
④ 横向·状态介入：BA_Damage/BA_Block → Hook.Resolve(...)；BA_SettlePerk → Hook.Settle(...)
```

**② 场景段级（`GameRun/.../PresentationBridge`）——“段域化”后沉淀的新四通道**

```
① 命令上行：UI 经注入的 XxxCommandDispatcher 提交语义意图(BuyCard/Choose/Proceed…) → 段 Loop/Session
② 事实下行：SegmentEventDispatcher<TEvent> 广播域事实(SE_*/EE_*/REE_*/AE_*) → UI 订阅刷新
③ 只读查询：UI 经 [Inject] RunState 读活体状态 / 或读快照事实(ShopShelfSnapshot)
④ 状态介入：段规则在 GameRun 域内经 RunState 受控入口改写整局/玩家状态
```

**通道 ③ 与 ④ 共用同一份 `Hook.Resolve`**，因此"卡面预览的伤害"与"实际扣掉的血"在结构上不可能不一致。六大场景阶段的表现层均严格走命令/事件/查询上行下行，未引入平行机制。

### 多场景阶段、地图与整局层

- **整局层 `RunState`**（`GameRun/Run`，纯 C#）：持有 `Floor`/`Rng`/`Player` 的遗物·药水·卡组等整局状态；数值变更以 `event Action` + `Creature` 的 `HpChanged`/`Healed` 下行通知 HUD。
- **地图系统**（`GameRun/Run/Map` + `Presentation/Run/TopBar/MapPanel`）：`StandardMapGenerator` 生成 7 列网格 DAG（拓扑→类型→剪枝→可达性→布局），全程同 RNG 可复现；问号节点经 `RunState.RollUnknownReveal` 按权重掷真实类型；`UI_MapView` 只读 `MapGraph`，选点经 `SegmentSceneManager` 转 `RunState.EnterMapNode` 受控入口。
- **阶段编排 `SegmentSceneManager`**（`Presentation/_Bootstrap/Run`）：按地图节点类型经 `RunFlow.SegmentKindOf` 分派 Additive 加载/卸载六阶段，无对下一阶段类型的硬引用；整局常驻靠 `RunLifetimeScope` + 叠加场景实现，**无 `DontDestroyOnLoad`**。
- **七个 `*LifetimeScope`**（Ancient/Rest/Chest/Shop/Event/Run + Battle）：结构一致，`Configure` 内只装配本阶段类型，各段 scope 互不引用。

### 核心纪律（新增代码请沿用）

- **状态变更只经动作系统。** `Creature` / `CardPile` 的所有写操作都在 `BattleAction.Execute` 里；整局数值只经 `RunState` 受控入口。
- **视图不持有业务规则、不直接篡改域状态。** 场景阶段 UI 经命令门面上发意图、订阅事实下行；`RunState` 的奖励兑现经显式注入引用完成（K1 例外，见下）。
- **配置只读一次。** 数值在烘焙期抽成运行时快照，运行时对象不持有配置引用。
- **失败路径也走事件。** 能量不足 / 卡已离场 → `BE_CardPlayRejected`，不静默丢弃。
- **事件一律 `struct` + 只读载荷**，零 GC。
- **状态数值唯一入口是 `Hook`。** 新增状态只需在 `BattlePerk` 子类实现 `GetModifier` 并在 `Hook` 登记分支。
- **预览与结算共用规则。** 卡面/瞄准的数值预览经 `BattleQueryDispatcher` 现算，绝不另写一份判定。
- **DI 组合根是唯一允许同时认识配置资产与运行时对象的地方。**
- **可复用件放对层。** 只依赖 UnityEngine 的通用工具 → `EngineUtility`；跨场景复用的表现件 → `CommonPresentation`；只有某阶段专属的 → `Presentation/<阶段>/`。
- **整局进度由 `RunState` 持有，表现层只读（受控写入口除外）。**

### 代码规模（本次评审工作树，`HEAD = 8eac818`）

| 域                   |      文件 |       行数 |
| ------------------- | ------: | -------: |
| Utility             |       7 |      247 |
| ConfigData          |     165 |    2,745 |
| RuntimeData         |      43 |    1,754 |
| EngineUtility       |      16 |      967 |
| GameBattle          |      77 |    2,482 |
| GameRun             |     138 |    4,976 |
| CommonPresentation  |      33 |    3,391 |
| Presentation        |     105 |    9,345 |
| **合计**              | **584** | **25,907** |

555 / 584 个文件在 150 行以内（95.0%）。超出的 29 个均为内聚单体：最大 `UI_HandView` 451 / `StandardMapGenerator` 423 / `UI_PotionGroup` 402 / `UI_ShopRugPanel` 387 / `UI_CardBattle` 366 / `UI_CardSelectPanel` 334 / `UI_MapView` 313。

---

## 当前开发状态

### 已完成并可运行

**战斗最小闭环（纵向切片已通）**

- 八阶段回合循环（战斗开始 → 玩家起 → 玩家操作 → 玩家结 → 敌方起 → 敌方行动 → 敌方结 → 战斗结束）
- 卡牌完整生命周期：抽牌 → 手牌 → 拖拽/瞄准 → 打出堆 → 词条结算 → 弃牌/消耗/遗忘离场
- 五个牌堆 + 局内卡组跨战斗保留
- 敌人意图系统：配置 → 烘焙 → 运行时意图 → 头顶图标预告 → 执行 → 推进；最前槽位自动替换
- 纵深阵型：0 号玩家 + 1~3 号敌人槽位，敌人可前进 / 回绕
- 状态（Perk）系统：数据驱动（`SO_Perk` + 5 具体，`PerkBaker`）；力量/敏捷经 `Hook` 修正伤害与格挡，虚弱/易伤参与乘性修正；再生经 `Hook.Settle` → `BA_Heal`
- 卡牌升级全链路：`UD_*` → `CardModelBaker` → `CM_Runtime.Upgrade()`；预览用 `ToMutable().Upgrade()` 副本
- 遗物系统（~51 运行时效果 / ~60 配置）：`RE_*` 经订阅战斗事件 + 组合 `BattleAction` 接入，`LizardTail` 经 `IDeathGuard` 介入致命伤，`RelicBaker` 类型分派 + `_ => throw` 兜底
- 药水系统（~15 种）：饮用经 `BC_UsePotion` → `PotionResolver` → `BA_ConsumePotion`，玩家持 3 槽
- **战后奖励结算**：`EncounterRewardRoller`（同种子可复现）→ `UI_RewardPanel` 展示并兑现

**「段域化」重构完成（本轮重大进展）**

- 六个场景阶段的**运行时逻辑全部从 `Presentation` 迁回 `GameRun` 域**：`AncientLoop`/`RestLoop`/`ShopSession`/`EventSession`/`ChestSession`/`BattleSession`，每段配 `PresentationBridge`（事实下行 + 命令门面）
- 表现层只经 `XxxCommandDispatcher` 上发语义意图、订阅 `SegmentEventDispatcher` 事实刷新，**不再直接改写 `RunState`/玩家数值**
- **HK-1 已闭环**：金币走 `RunState.SpendGold/GainGold` 受控入口、HP 变更经 `Creature.CurrentHp` setter 抛 `HpChanged`/`Healed` 通知（HUD 同步隐患消除）；`FindObjectOfType` 由 5 降 0
- `RunState` 暴露完整受控写入口（`GainGold`/`SpendGold`/`RestorePlayerHp`/`SpendPlayerHp`/`AddCard`/`AddRelic`/`TryAddPotion`/`EnterMapNode`/`CompleteMapNode`）

**跑图地图系统落地（本轮新增）**

- `StandardMapGenerator`：7 列网格 DAG，拓扑→类型→剪枝→可达性→布局，同 RNG 可复现；`IsRowLegal` 行规则 + `RunState.RollUnknownReveal` 问号节点掷真实类型
- `UI_MapView`（313 行）只读 `MapGraph`，选点经 `SegmentSceneManager` 转 `RunState.EnterMapNode` 受控入口；`RunFlow.SegmentKindOf` 把节点类型分派到六段

**六个场景阶段（同构零漂移）**

- 先古之民段 / 休息段（含 Cook/Dig/Forge/Heal 四选项，数据驱动）/ 战斗段 / 宝箱段（第四阶段）/ 商店段（第五阶段，`SO_ShopPrices` + 货架快照 `SE_ShelfUpdated`）/ 事件段（第六阶段，`SO_Event` + `EED_*` 效果数据多态 + 5 具体事件）
- 七段 `*LifetimeScope` 各自隔离装配；商店/事件/休息/事件配置系统均为数据驱动

**表现层（已成型）**

- Spine 动画演出（攻击 / 受击 / 死亡 / 施法，播到 50% 放行事件链）
- 手牌区（弧形排布、悬停偏移、拖拽阻尼、瞄准定住）、拖拽瞄准（弧长曲线 + 整支变红）
- 牌堆三堆计数步进 + 弹出查看、伤害/回血飘字与一次性特效、状态特效、卡面类型/稀有度/不可打/数值着色/关键词悬停
- 各段表现：对话/选项、铁匠、宝箱 Spine、商店货架、事件选项、整局顶栏（卡组/地图/菜单/遗物/药水栏）

**已有内容资产**

- 卡牌 8 张、敌人 1 种（树枝史莱姆）+ 玩家 1 套、状态 5 种、词条 10 + 升级补丁 4 + 意图 6
- 遗物 ~51 运行时效果（Anchor / BurningBlood / LizardTail / MeatCleaver / Orichalcum 等）/ 药水 ~15 / 事件 5 / 休息选项 4
- 场景 6 阶段 + 整局层；预制体若干（卡面、意图图标、单位、弹层、特效、面板）

### 近期落地（2026-10 下旬至本轮）

- **「段域化」重构**：六段 UI 改经命令门面上发、订阅事实下行；段规则下沉 `GameRun` 域；HK-1 闭环、HP 通知闭合、`FindObjectOfType` 清零。
- **跑图地图系统**：`StandardMapGenerator` + `MapGraph`/`MapNode` + `UI_MapView`，同 RNG 可复现，`RunFlow` 分派节点→段。
- **六场景阶段同构落地**（Ancient/Rest/Chest/Shop/Event/Battle）+ 事件/休息配置数据驱动系统 + 跨段弹板基类合并（去重约 390 行）。
- **内容资产续增**：遗物运行时效果 51、药水 ~15、事件 5、休息选项 4、商店价格、地图配置。
- **H-1 彻底解决（往期）**：删除 `GameEvent`/`GameRest`/`GameShop` 三个空骨架 asmdef。
- **分层澄清（往期）**：`CommonPresentation` 与 `Presentation` 平级、`Presentation` 依赖 `CommonPresentation` 为故意设计；`UI_RewardPanel` 经注入 `RunState` 兑现奖励在允许方向上。

### 已知未完成 / 待收口（功能完整性 + 架构，非全部为缺陷）

| 项 | 状态 |
| --- | --- |
| ~~**HK-1｜商店/事件段规则写在 `Presentation` 域**~~ | **已解决（本轮）**：段逻辑已迁回 `GameRun`，UI 经命令门面上发；金币/HP 走 `RunState` 受控入口，HP 变更经 `Creature` setter 通知 |
| **K1｜`UI_RewardPanel`（`CommonPresentation`）直接调 `RunState` 受控方法** | 跨段发奖解析器，走受控入口、非字段裸写；与设计方共识为接受的复用取舍，但偏离「表现层只读」。建议经命令门面转发，爆破半径 1 文件 |
| **K2｜地图选点 / 战斗外用药绕过段命令门面** | `SegmentSceneManager`/`UI_PotionGroup` 直接调 `RunState` 受控方法——一致性缺口，非耦合违例；建议收口到对应 Dispatcher |
| **K3｜`RaiseMaxHp` 绕过 `RunState` 入口** | `AncientLoop`/`RestLoop` 直接调 `Creature.RaiseMaxHp`（仍经 setter 通知），建议补 `RunState.RaiseMaxHp` |
| **K4｜1 处 `StartCoroutine`** | `UI_MapView` 地图选点延迟回调，可改 UniTask/DOTween |
| **K5｜`UI_SingletonObject` 死代码** | `EngineUtility/UI/UI_SingletonObject`（`DontDestroyOnLoad` 助手，零引用），建议删除 |
| **K6｜`StatHook`/`SettleHook` 手动 `switch` 分派** | 新增状态需改两处，评估注册表分派 |
| **K7｜29 文件 >150 行** | 经核验均内聚、非 SRP 违例；新功能优先拆小组件 |
| **K8｜`TODO`×4 / `Debug.Log`×29** | 发布前收敛 |
| 战斗结束奖励（战败分支） | 胜利分支已打通；**战败分支仍未接**（照旧立即收段） |
| 卡牌升级正式入口 | 升级数据→烘焙→运行时应用全链路已通；"升级获取途径 / 升级 UI 入口"仍未规划 |
| 商店 / 金币闭环 | 消费端已就位（金币字段、牌堆视图可复用），金币循环待与事件/地图打通 |
| 遗物 / 药水（局外持有 / 掉落 / 获取途径） | 战斗内效果已通、`RunState` 持有已就位；商店 / 事件等其它获取途径未做 |
| 格挡 / 治疗意图数值 | 数据层有 `Amount`，意图图标为占位，玩家无法预判敌人会挡多少 / 回多少 |
| 音效 | 代码中已留触发点 TODO，音效层尚未开写 |
| `BattleEventDispatcher.Unregister` | 单位视图销毁后事件处理器仍滞留（单场战斗内有界，靠 null 检查兜住） |
| `UI_OneShotVfx` 魔法字符串 | `_clip.name` 当 Animator 状态名（L-1），动画状态改名即静默失效 |

---

## 后续计划

> 按依赖顺序排列，前项未完成时后项会返工。单人开发，按"先让已有数据生效、再扩系统、同时修架构异味"推进。

### 阶段一：收口当前重构（已完成 ✅）

1. ✅ 手牌区四组件各司其职。
2. ✅ 通用对象池与可复用表现件抽取。
3. ✅ 状态数值系统闭环（`Hook` + `ModifiableStat`）。
4. ✅ 卡牌升级子系统全链路打通。
5. ✅ 三新场景阶段拆出，跨场景复用边界建立。
6. ✅ 删除 `GameEvent`/`GameRest`/`GameShop` 三个空骨架（H-1）。
7. ✅ 宝箱段 / 商店段 / 事件段 / 休息段 / 先古之民段同构落地。
8. ✅ 跨段弹板基类合并（去重约 390 行）。
9. ✅ **「段域化」重构 + HK-1 闭环 + 地图系统落地**。

### 阶段二：补齐战斗内功能空洞 + 收口架构异味（最高优先级）

1. ✅ **HK-1 收口**：段规则下沉 `GameRun`，UI 经命令门面上发；HP 经 `Creature` setter 通知。
2. **K1/K2 一致性收口**：`UI_RewardPanel` 经命令门面转发；地图选点 / 战斗外用药收口到对应 Dispatcher。
3. **战斗结束奖励流程**：胜利 / 失败分支、奖励展示与选择、结果落回 `RunState`。
4. `BattleEventDispatcher` 增 `Unregister`，单位视图 `OnDestroy` 反注册。
5. 敌方意图图标补数值展示（与卡面 `CardValueRenderer` 同源）。
6. 升级正式入口：升级 UI + 获取途径（事件 / 遗物 / 商店）。
7. 音效层 `Presentation/Feedback`：订阅 `BE_*` 播发声。

### 阶段三：可扩展性加固

1. `PerkBaker`/`CardModelBaker`/`IntentBaker`/`Hook.Settle` 的 `switch` 加 `default` 报错（`RelicBaker` 已先行）。
2. 清理未使用依赖、删除死代码 `UI_SingletonObject`、修 `UI_OneShotVfx` 的 `_clip.name` 魔法字符串（L-1）。
3. 阵型上限收进 `BattleSlots`，与槽位定义同源。
4. 分派守卫抽 `ReentrancyGuard`/`RunOnceGuard`，区分 `IDeathGuard` 命名。
5. `StatHook`/`SettleHook` 改注册表分派，消除手动 `switch` 摩擦（K6）。

### 阶段四：新系统（视毕设进度决定是否开写）

1. ~~**局内地图与楼层编排**~~ —— **已完成**（阶段一 #9 地图系统落地）。
2. **遗物系统（局外闭环）**：获取 / 持有 / 掉落与跑图进度循环。
3. **商店与金币闭环**：消费端已就位，打通获取与掉落后形成经济循环。

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

> 注：当前 6 个场景阶段由 `SegmentSceneManager` 按地图节点叠加加载；直接打开 `Scene_Battle` 也能独立运行战斗段（依赖注入由 `BattleLifetimeScope` 接管）。

---

## 文档

| 文档 | 内容 |
| --- | --- |
| [Doc/架构分析报告.md](Doc/架构分析报告.md) | 逐文件架构评审：综合评分 96/100、六维分项评分、逐模块分析、反模式扫描、不变量核验、问题清单 |
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
