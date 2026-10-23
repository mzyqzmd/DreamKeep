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

一句话总纲：**五个程序集、一张无环依赖 DAG、四通道解耦（命令上行 / 事件下行 / 只读查询 / 状态介入）；配置只读一次即烘焙为运行时对象，规则与状态只存在一处，表现层只读不回写。**

> 详细逐文件评审见 [Doc/架构分析报告.md](Doc/架构分析报告.md)（综合评分 **93 / 100**）。

### 程序集与依赖（asmdef 强制，已用 GUID 反解核验无环）

```
Utility ─────────────┐
ConfigData ──────────┤   （两者均为叶层，仅依赖外部包，互不为依赖）
        │            │
RuntimeData ──→ {Utility, ConfigData}
        │
GameBattle ───→ {Utility, RuntimeData, ConfigData}
        │
Presentation ─→ {Utility, RuntimeData, ConfigData, GameBattle}  (+ 8 个外部包)
```

`ConfigData` 是纯资产/数据层，**不依赖 `Utility`**——实际是**扇形汇入**而非单链。五个程序集构成有向无环图，编译期即生效，无任何反向引用或环。

### 五个域

| 程序集              | 职责          | 关键内容                                                                                                                   |
| ---------------- | ----------- | ---------------------------------------------------------------------------------------------------------------------- |
| **Utility**      | 通用件（零业务知识）  | 状态机 `StateMachine<T>`、确定性随机 `Rng`、可修正数值引擎 `ModifiableStat`、UI 几何 `UiGeometry`、补间 `UiTween`、组件池 `MonoObjectPool<T>`、纯 C# 池 `ObjectPool<T>` |
| **ConfigData**   | 配置资产（策划可编辑） | `SO_Player` / `SO_Enemy` / `SO_Card`、**状态资产 `SO_Perk` + 5 个具体 `SO_*`**、意图/词条/升级补丁数据 `UpgradeData`/`UD_*`——只有"做什么、做多少"，零执行逻辑          |
| **RuntimeData**  | 运行时数据（纯 C#） | `Creature` / `Enemy` / `Player` / `PlayerCombatState` / `CardModel` / `CardPile` / `EnemyIntent` / `CardAffix`、状态运行时 `BattlePerk`/`BP_*` + `PerkBaker`、三个烘焙器 |
| **GameBattle**   | 战斗域（规则与流程）  | 八阶段状态机、动作系统 `ActionSystem` + 27 个 `BattleAction`、**状态介入层 `Hook`**、两个 Resolver、命令/事件/**查询**三桥                                  |
| **Presentation** | 表现层（只读域侧）   | 单位视图 / 卡牌视图 / HUD / 弹层 / 目标提示、演出编排、DI 组合根 `BattleLifetimeScope`、启动簇 `Bootstrap`、弹层中枢 `PopRoot`、查询门面消费方                       |

### 四通道通信（表现层 ↔ 战斗逻辑）

```
① 上行·命令（玩家意图）
  视图 → BattleCommandDispatcher → BattleLoop.EnqueueCommand → 等待态阶段取出执行（只入队，不直接驱动）

② 下行·事件（域侧结果）
  BattleAction.Execute → List<IBattleEvent> → BattleEventDispatcher.PlayAsync → 各视图处理器并行演出

③ 上行·只读查询（预览，无副作用）
  表现层 → BattleQueryDispatcher.(PreviewTargets/PreviewCardDamage/PreviewCardBlock/…) → 直算返回（与结算共用同一份规则）

④ 横向·状态介入（规则层内）
  BA_Damage/BA_Block → Hook.Resolve(...) 求最终值；BA_SettlePerk → Hook.Settle(...) 收集状态结算动作
```

**核心纪律**（新增代码请沿用）：

- **状态变更只经动作系统。** `Creature` / `CardPile` / `PlayerCombatState` 的所有写操作都在 `BattleAction.Execute` 里。
- **视图只读不回写。** 表现层不持有业务规则、不直接调阶段方法。
- **配置只读一次。** 数值在烘焙期抽成运行时快照，运行时对象不持有配置引用。
- **失败路径也走事件。** 能量不足 / 卡已离场 → `BE_CardPlayRejected`，不静默丢弃。
- **事件一律 `struct` + 只读载荷**，零 GC。
- **状态数值唯一入口是 `Hook`。** 任何「状态如何影响伤害/格挡/结算」都走 `Hook` 分派表，新增状态只需在 `BattlePerk` 子类实现 `GetModifier` 并在 `Hook` 登记分支。
- **预览与结算共用规则。** 卡面/瞄准的数值预览经 `BattleQueryDispatcher` 现算，绝不另写一份判定。
- **DI 组合根是唯一允许同时认识配置资产与运行时对象的地方。**
- **手牌区四组件各司其职。** `UI_HandView`（编排 + 每帧驱动）/ `UI_HandHover`（悬停裁决）/ `UI_HandAim`（瞄准客户端 + 数值预览）/ `UI_HandShow`（事件演出）同节点协作。
- **瞄准是战斗通用件。** 箭头与瞄准流程住 `Presentation/Battle/Targeting/`：`BattleTargetDirector` 独占唯一箭头与单客户端槽位，任何"拖出去指向目标"的交互件实现 `IAimClient` 即可接入（手牌是第一个客户端，药水将来同一个接口）。

### 代码规模（2026-10-19 工作树）

| 域            |      文件 |       行数 |
| ------------ | ------: | -------: |
| Utility      |      14 |      544 |
| ConfigData   |      38 |      597 |
| RuntimeData  |      39 |     1509 |
| GameBattle   |      69 |     2027 |
| Presentation |      42 |     4051 |
| **合计**       | **202** | **8728** |

194 / 202 个文件在 150 行以内；超出的 8 个均为表现层内聚单体（`UI_HandView` 416 / `UI_CardBattle` 391 / `UI_HandHover` 200 / `UI_HandShow` 198 / `UI_Actor` 189 / `UI_CardFace` 230 / `BattlePopRoot` 149 / `BattleTargetDirector` 159）。

---

## 当前开发状态

### 已完成并可运行

**战斗最小闭环（纵向切片已通）**

- 八阶段回合循环（战斗开始 → 玩家起 → 玩家操作 → 玩家结 → 敌方起 → 敌方行动 → 敌方结 → 战斗结束）
- 卡牌完整生命周期：抽牌 → 手牌 → 拖拽/瞄准 → 打出堆 → 词条结算 → 弃牌/消耗/遗忘离场
- 五个牌堆（抽牌堆 / 手牌 / 打出堆 / 弃牌堆 / 消耗堆）+ 局内卡组跨战斗保留
- 敌人意图系统：意图配置 → 烘焙 → 运行时意图 → 头顶图标预告 → 执行 → 推进下一条
- 纵深阵型：0 号玩家 + 1~3 号敌人槽位，敌人可前进/回绕，槽位占用判定
- 状态（Perk）系统：**已数据驱动**（配置 `SO_Perk` + 5 个具体资产，运行时 `BattlePerk`/`BP_*` 经 `PerkBaker` 烘焙）；结算时机声明（回合开始 / 回合结束 / 常驻）、施加与叠加、结算时先触发效果再递减层数、归零移除
- **状态数值已接入结算**：力量/敏捷经 `Hook` 修正伤害与格挡，虚弱/易伤参与伤害修正；再生在回合边界经 `Hook.Settle` → `BA_Heal` 结算（敌我共用）
- 完整表现层：Spine 动画演出（攻击/受击/死亡/施法，非全阻塞并行）、血条滞后条、格挡显隐、状态槽位、意图气泡、目标高亮、手牌弧形排布与悬停避让、拖拽瞄准箭头、弃牌波与洗回波演出、**受击/回复数字飘字（`UI_NumberPopTip` + `PopRoot`）**

**近期落地（2026-10 中下旬）**

- **状态数值系统闭环**：`ModifiableStat` 重写为「加值 + 乘率 + 上下限折叠」引擎并支持池化/克隆；`Hook` 成为状态→数值/动作的唯一入口；`BA_Damage`/`BA_Block` 接入状态修正——力量/虚弱/易伤从「无效」变「有效」。
- **卡牌升级子系统全链路打通**：`UpgradeData`/`UD_*` 配置 → `CardModelBaker` 解析为 `CM_ValuePatch` → `CM_Runtime.Upgrade()` 现改数值/追加词条/移除词条 → `CardModel.Upgrade()` 已可从 `UI_CardFace` 调用。
- **查询门面 `BattleQueryDispatcher`**：表现层只读提问出口，预览与结算同源，新增「状态介入 / 只读查询」两个干净架构原语。
- **弹层中枢 `PopRoot` + 数字飘字 `UI_NumberPopTip`**：统一管理说明气泡列与伤害/回血/格挡/回合横幅飘字，全部池化复用。
- 手牌区三组件（编排/悬停裁决/事件演出）各司其职；通用对象池 `MonoObjectPool<T>` 与纯 C# `ObjectPool<T>` 双池齐备。

**已有内容资产**

- 卡牌 6 张（Attack / Bash / Block / ForgottenRitual / PommelStrike / SwordBoomerang）
- 敌人 1 种（树枝史莱姆 TwigSlime，含四条意图 + 最前槽位替换意图）、玩家 1 套
- 状态 5 种（再生 / 力量 / 敏捷 / 易伤 / 虚弱，均数据驱动）
- 词条 10 种 + 升级补丁 4 种
- 预制体若干（卡面 4 种、意图图标 5 种、单位 2 种、面板与弹层）
- 编辑器工具集 ToolBoxHub（文件夹着色、层级图标等）

### 已知未完成（功能完整性，非架构缺陷）

| 项                            | 状态                                                                                                           |
| ---------------------------- | ------------------------------------------------------------------------------------------------------------ |
| **战斗结束奖励**                 | `BP_BattleFinish.HandleCommand` 仍是 `TODO`（等待玩家选择奖励发命令）                                                    |
| 卡牌升级正式入口                  | 升级数据→烘焙→运行时应用全链路已通；但「升级获取途径 / 升级 UI 入口」（事件 / 遗物 / 商店）仍未规划，玩家当前无正式方式触发升级                          |
| 商店 / 金币                     | `SO_Player._startGold` 已定义，零消费                                                                               |
| `BattleEventDispatcher.Unregister` | 单位视图销毁后事件处理器仍留在字典（当前靠表现层 null 检查兜住，无实际 bug；建议 `UI_Actor` 销毁时一并反注册）                       |
| 敌方意图图标数值展示                | 卡面伤害/格挡已显示状态修正后的最终值；敌方意图图标是否展示数值待确认                                                          |
| 遗物 / 药水槽 / 法球槽               | `Player` 类注释里记名不落字段，共 7 项待落地                                                                                 |
| 单局内的地图 / 楼层 / 敌人编排           | 战斗是唯一场景（`Scene_Battle.unity`），跑图系统未开写                                                                        |

---

## 后续计划

> 按依赖顺序排列，前项未完成时后项会返工。单人开发，按"先让已有数据生效、再扩系统"推进。

### 阶段一：收口当前重构（已完成 ✅）

1. ✅ 手牌区三组件各司其职，`UI_HandView` 降到 518 行、无重复逻辑。
2. ✅ 通用对象池与可复用说明气泡抽取。
3. ✅ 状态数值系统闭环（`Hook` + 重写 `ModifiableStat`），力量/虚弱/易伤/敏捷/再生全部接入。
4. ✅ 卡牌升级子系统全链路打通。

### 阶段二：补齐战斗内的功能空洞（最高优先级）

1. **战斗结束奖励流程**：填 `BP_BattleFinish.HandleCommand`（胜利/失败后的奖励与流程分支）。
2. `BattleEventDispatcher` 增 `Unregister`，单位视图在 `OnDestroy` 反注册事件处理器。
3. `BattleQueryDispatcher` / `CardValueRenderer` 接入"贡献者展开"（如「9（力量+2）」）的预留扩展点。
4. 敌方意图图标补数值展示（与卡面 `CardValueRenderer` 同源）。
5. 升级正式入口：升级 UI + 获取途径（事件 / 遗物 / 商店三选一）。

### 阶段三：可扩展性加固

1. `CardAffixResolver` / `EnemyIntentResolver` / `Hook` / `PerkBaker` / `CardModelBaker` 的 `switch` 加显式 `default` 报错，把"忘加 case"从静默失效变成可见失败。
2. 阵型上限等硬编码收进一处常量；`CardPile.Cards` 改为只读暴露。
3. 数据驱动状态后，补充状态资产的设计文档与示例资产。

### 阶段四：新系统（视毕设进度决定是否开写）

1. **局内地图与楼层编排**：新开 `GameRun` 程序集，方向 `GameRun → GameBattle`（战斗不认识跑图）。
2. **遗物系统**：与升级、商店一起做，作为局内成长的主承载。
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
| [Doc/架构分析报告.md](Doc/架构分析报告.md) | 逐文件架构评审：综合评分 93/100、分项维度评分、分模块分析、全量文件清单、反模式扫描、已知缺口 |
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
