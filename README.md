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

一句话总纲：**五个程序集、一张无环依赖 DAG、三段式通信；配置只读一次即烘焙为运行时对象，规则与状态只存在一处，表现层只读不回写。**

> 详细逐文件评审见 [架构分析报告.md](架构分析报告.md)（综合评分 **91 / 100**）。

### 程序集与依赖（asmdef 强制，已用 GUID 反解核验无环）

```
Utility ─────────────┐
ConfigData ──────────┤   （两者均为叶层，仅依赖外部包，互不为依赖）
        │            │
RuntimeData ──→ {Utility, ConfigData}
        │
GameBattle ───→ {Utility, RuntimeData, ConfigData}
        │
Presentation ─→ {Utility, RuntimeData, ConfigData, GameBattle}  (+ 外部包)
```

`ConfigData` 是纯资产/数据层，**不依赖 `Utility`**——过去文档里「`Utility ← ConfigData` 线性链」的表述不准确，实际是**扇形汇入**而非单链。五个程序集构成有向无环图，编译期即生效，无任何反向引用或环。

### 五个域

| 程序集              | 职责          | 关键内容                                                                                                                   |
| ---------------- | ----------- | ---------------------------------------------------------------------------------------------------------------------- |
| **Utility**      | 通用件（零业务知识）  | 状态机 `StateMachine<T>`、确定性随机 `Rng`、可修正数值 `ModifiableStat`、UI 几何 `UiGeometry`、补间工具 `UiTween`、通用组件池 `MonoObjectPool<T>`   |
| **ConfigData**   | 配置资产（策划可编辑） | `SO_Player` / `SO_Enemy` / `SO_Card`、意图配置 `IntentData`、词条配置 `AffixData`、状态配置 `BattlePerk`——只有"做什么、做多少"，零执行逻辑           |
| **RuntimeData**  | 运行时数据（纯 C#） | `Creature` / `Enemy` / `Player` / `PlayerCombatState` / `CardModel` / `CardPile` / `EnemyIntent` / `CardAffix`，以及三个烘焙器 |
| **GameBattle**   | 战斗域（规则与流程）  | 八阶段状态机、动作系统 `ActionSystem` + 25 个 `BattleAction`、两个 Resolver、命令/事件桥                                                    |
| **Presentation** | 表现层（只读域侧）   | 单位视图 / 卡牌视图 / HUD / 弹层 / 目标提示、演出编排、DI 组合根 `BattleLifetimeScope`                                                        |

### 三段式通信（表现层 ↔ 战斗逻辑）

```
上行（玩家意图）
  视图 → BattleCommandDispatcher → BattleLoop.EnqueueCommand → 等待态阶段取出并执行（只入队，不直接驱动）

下行（域侧结果）
  BattleAction.Execute → List<IBattleEvent> → BattleEventDispatcher.PlayAsync → 各视图处理器并行演出
```

**核心纪律**（新增代码请沿用）：

- **状态变更只经动作系统。** `Creature` / `CardPile` / `PlayerCombatState` 的所有写操作都在 `BattleAction.Execute` 里。
- **视图只读不回写。** 表现层不持有业务规则、不直接调阶段方法。
- **配置只读一次。** 数值在烘焙期抽成运行时快照，运行时对象不持有配置引用。
- **失败路径也走事件。** 能量不足 / 卡已离场 → `BE_CardPlayRejected`，不静默丢弃。
- **事件一律 `struct` + 只读载荷**，零 GC。
- **DI 组合根是唯一允许同时认识配置资产与运行时对象的地方。**
- **手牌区三组件各司其职。** `UI_HandView`（编排）/ `UI_HandHover`（悬停裁决）/ `UI_HandShow`（事件演出）同节点协作，重复逻辑不跨组件。

### 代码规模（2026-10-15 工作树）

| 域            |      文件 |       行数 |
| ------------ | ------: | -------: |
| Utility      |      12 |      418 |
| ConfigData   |      32 |      533 |
| RuntimeData  |      28 |     1081 |
| GameBattle   |      65 |     1839 |
| Presentation |      39 |     3529 |
| **合计**       | **176** | **7400** |

169 / 176 个文件在 150 行以内；超出的 7 个均为表现层内聚单体（`UI_HandView` 493 / `UI_CardBattle` 330 / `UI_HandHover` 231 / `UI_HandShow` 222 / `UI_Actor` 189 / `UI_CardFace` 179 / `UI_HoverTip` 153）。

---

## 当前开发状态

### 已完成并可运行

**战斗最小闭环（纵向切片已通）**

- 八阶段回合循环（战斗开始 → 玩家起 → 玩家操作 → 玩家结 → 敌方起 → 敌方行动 → 敌方结 → 战斗结束）
- 卡牌完整生命周期：抽牌 → 手牌 → 拖拽/瞄准 → 打出堆 → 词条结算 → 弃牌/消耗/遗忘离场
- 五个牌堆（抽牌堆 / 手牌 / 打出堆 / 弃牌堆 / 消耗堆）+ 局内卡组跨战斗保留
- 敌人意图系统：意图配置 → 烘焙 → 运行时意图 → 头顶图标预告 → 执行 → 推进下一条
- 纵深阵型：0 号玩家 + 1~3 号敌人槽位，敌人可前进/回绕，槽位占用判定
- 状态（Perk）系统：施加/叠加/层数递减/归零移除，灼烧与再生已接入回合结算
- 完整表现层：Spine 动画演出（攻击/受击/死亡/施法，非全阻塞并行）、血条滞后条、格挡显隐、状态槽位、意图气泡、目标高亮、手牌弧形排布与悬停避让、拖拽瞄准箭头、弃牌波与洗回波演出

**近期收口与优化（2026-10-13 ~ 2026-10-15）**

- **手牌区重构已收口**：`UI_HandView`（493 行编排器）+ 同节点 `UI_HandHover`（悬停裁决 / 避让 / 词条气泡对账）+ `UI_HandShow`（11 条手牌事件演出）三组件各司其职，旧重复代码已清除，无双重演出。
- **通用对象池 `MonoObjectPool<T>` 抽取**至 `Utility/Engine/Pool`，卡牌飞行统一复用。
- **说明气泡发起器 `UI_HoverTip` 抽成可复用组件**（Pointer / External 两种触发、静态/动态两种内容），卡牌词条提示与单位信息均可挂载。
- 修复手牌悬停漂移 bug、调整手牌参数；抽牌入场改为数字弹跳、悬停对齐指针、拖拽阻尼跟随；玩家与敌人动画同时演绎非全阻塞。

**已有内容资产**

- 卡牌 6 张（Attack / Bash / Block / ForgottenRitual / PommelStrike / SwordBoomerang）
- 敌人 1 种（树枝史莱姆 TwigSlime，含四条意图 + 最前槽位替换意图）、玩家 1 套
- 状态 5 种（灼烧 / 再生 / 力量 / 易伤 / 虚弱）
- 词条 10 种
- 预制体若干（卡面 4 种、意图图标 5 种、单位 2 种、面板与弹层）
- 编辑器工具集 ToolBoxHub（文件夹着色、层级图标等）

### 已知未完成（功能完整性，非架构缺陷）

| 项                            | 状态                                                                                                           |
| ---------------------------- | ------------------------------------------------------------------------------------------------------------ |
| **力量 / 虚弱 / 易伤 三个状态未接入伤害结算** | 配置、图标、描述、`Clone` 全有，但 `BA_Damage` 直接按基础值结算、不查修正（`BA_Damage` 注释已标注『伤害修正为后续扩展点』）→ **玩家拿到这些状态当前无任何效果**。当前最大功能缺口 |
| 战斗结束奖励                       | `BP_BattleFinish.HandleCommand` 是空实现 + TODO                                                                  |
| 卡牌升级                         | `SO_Card` 的升级词条集已烘焙、`CM_Runtime.Upgrade()` 已实现，但无调用方（缺升级 UI 与获取途径）                                           |
| 修正系统                         | `ModifiableStat.Add` / `Remove` 已实现，全项目零调用（待伤害结算接入时启用）                                                       |
| 商店 / 金币                      | `SO_Player._startGold` 已定义，零消费                                                                               |
| 遗物 / 药水槽 / 法球槽               | `Player` 类注释里记名不落字段，共 7 项待落地                                                                                 |
| 格挡 / 治疗意图图标不显示数值             | 占位视图类，玩家无法预判敌人数值                                                                                             |
| 敌方意图不结算敌人身上的灼烧 / 再生          | 玩家侧已接入，敌方侧只做层数衰减                                                                                             |
| 单局内的地图 / 楼层 / 敌人编排           | 战斗是唯一场景（`Scene_Battle.unity`），跑图系统未开写                                                                        |
| 事件订阅只增不减                     | `BattleEventDispatcher` 无 `Unregister`，单位视图销毁后处理器仍留在字典里（当前靠表现层 null 检查兜住，无实际 bug）                            |
| 最后一处场景查找                     | `UI_HoverTip` 用 `FindFirstObjectByType<UI_PopTextRoot>()` 懒查弹层宿主（静态缓存 + 伪空重查），建议改为 `[Inject]` 注入以彻底零全局查找     |

---

## 后续计划

> 按依赖顺序排列，前项未完成时后项会返工。单人开发，按"先让已有数据生效、再扩系统"推进。

### 阶段一：收口当前重构（已完成 ✅）

1. ✅ 删除 `UI_HandView` 中已被 `UI_HandHover` / `UI_HandShow` 取代的重复方法与字段，`UI_HandView` 降到 493 行、三组件各司其职。
2. ✅ 通用对象池与可复用说明气泡抽取。
3. ✅ 演出时长参数统一归到 `UI_HandShow`、修复悬停漂移等手感问题。

### 阶段二：补齐战斗内的功能空洞（最高优先级）

1. **`BA_Damage` 接入状态修正**：`BP_Strength` 加值、`BP_Weak` 减伤、`BP_Vulnerable` 增伤，与 `ModifiableStat` 的"先加值再乘算"语义对齐。一行改动让三个状态从无效变有效，玩家立刻可感知——**收益最高**。
2. 敌人意图结算接入敌人身上的灼烧 / 再生。
3. 格挡 / 治疗意图图标补数值展示。
4. 战斗结束奖励流程（填 `BP_BattleFinish.HandleCommand`）。
5. `UI_HoverTip` 的弹层宿主改为 `[Inject]` 注入，消除最后一处场景查找。

### 阶段三：可扩展性加固

1. `CardAffixResolver` / `EnemyIntentResolver` 的 `switch` 加显式 `default` 报错，把"忘加 case"从静默失效变成可见失败。
2. `BattleEventDispatcher` 增 `Unregister`，单位视图在 `OnDestroy` 反注册。
3. 阵型上限等硬编码收进一处常量；`CardPile.Cards` 改为只读暴露。

### 阶段四：新系统（视毕设进度决定是否开写）

1. **卡牌升级链路**：升级 UI + 升级获取途径（事件 / 遗物 / 商店三选一）。
2. **局内地图与楼层编排**：新开 `GameRun` 程序集，方向 `GameRun → GameBattle`（战斗不认识跑图）。
3. **遗物系统**：与升级、商店一起做，作为局内成长的主承载。
4. **商店与金币**：消费端已就位（金币字段、抽牌堆/牌堆视图可复用）。

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

| 文档                                                                                 | 内容                                                 |
| ---------------------------------------------------------------------------------- | -------------------------------------------------- |
| [架构分析报告.md](架构分析报告.md)                                                             | 逐文件架构评审：综合评分 91/100、分项维度评分、分模块分析、全量文件清单、反模式扫描、已知缺口 |
| [.codebuddy/Docs/03-毕业设计方案-卡牌肉鸽融合成熟玩法.md](.codebuddy/Docs/03-毕业设计方案-卡牌肉鸽融合成熟玩法.md) | 纯游戏设计稿（v3.5）：玩法 / 数值 / 题材定案，不含实现                   |
| [.codebuddy/Docs/04-项目实现顺序与系统解耦设计.md](.codebuddy/Docs/04-项目实现顺序与系统解耦设计.md)         | 架构定案与 P0 待办清单：「代码放哪、依赖谁、先做什么」以此为准                  |

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
