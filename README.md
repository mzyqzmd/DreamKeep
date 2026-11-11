# 尖塔梦境 · Dream Spire

> 一款以「梦之尖塔」为舞台的单人 Roguelike 卡牌构筑游戏，基于 Unity 2022.3 LTS。
> 工程代号 **DreamKeep**，当前版本 `v0.1`（开发中，纵向切片已可运行）。

沉睡者的意识世界里，晨门被封，被困的意识化作梦魇沿梦境走廊逼近。你是**守梦人**——沉眠中唯一清醒的自我。沿塔一层层向上净化梦魇，重新打开晨门，让黎明降临；每一次力竭都只是从浅梦处重新醒来。

> **关于命名**：`DreamKeep` 是技术代号（仓库名、项目根目录、程序集名 `_DreamKeep.*`）；`尖塔梦境 / Dream Spire` 是游戏名（设定文档与对外展示，Player Settings 中登记为 `DreamSpire`）。二者有意解耦，详见 [Doc/世界观与背景设定.md](Doc/世界观与背景设定.md) §0。

---

## 快速开始

```bash
git clone https://github.com/mzyqzmd/DreamKeep.git
cd DreamKeep
git lfs install   # 首次必须：美术/音频/动画走 LFS，未安装会签出成指针文件
git lfs pull
```

1. Unity Hub → `Add project from disk` → 选择本仓库根目录（用 `2022.3.62f3c1` 打开）
2. 等待 `Library/` 首次导入完成
3. 打开 `Assets/Scenes/Scene_MainMenu.unity` 运行 —— 这是整局入口，会按地图选点叠加编排六个阶段
4. 只想单独看战斗：直接打开 `Assets/Scenes/Scene_Battle.unity`（依赖注入由 `BattleLifetimeScope` 接管）

八个场景均已登记在 Build Settings 中：主菜单、整局层，以及先古之民 / 休息 / 战斗 / 宝箱 / 商店 / 事件六段。

---

## 现在能玩到什么

从主菜单新开一局，到走完一轮地图节点，主闭环已通：**地图选点 → 进入阶段 → 结算奖励 → 回到地图 → 存档续局**。尚未做数值平衡与内容填量。

| | 已落地 |
| --- | --- |
| **整局** | 7 列网格 DAG 地图（同种子可复现）、问号节点按权重揭示、六阶段按节点类型叠加加载、整局顶栏（卡组 / 遗物 / 药水 / 地图 / 菜单）、三处检查点存档与读档续局 |
| **战斗** | 八阶段回合循环；能量—手牌—意图循环；五个牌堆与局内卡组跨战斗保留；敌人意图预告与推进；纵深阵型（0 号玩家 + 1~3 号敌人槽位，敌人可前进 / 回绕）；前排优先级与敌人前进触发特殊意图 |
| **构筑** | 状态（力量 / 敏捷 / 虚弱 / 易伤 / 再生）经 `Hook` 修正数值；卡牌词条与升级链路；约 60 种遗物配置、约 15 种药水、5 个事件、4 个休息选项、双阶段 Boss；战后奖励掷骰与兑现 |
| **表现** | Spine 攻击 / 受击 / 死亡 / 施法演出；弧形手牌、悬停偏移、拖拽阻尼与弧长瞄准；牌堆计数、伤害 / 回血飘字、一次性特效；卡面稀有度与数值着色、关键词悬停；商店货架、事件选项、宝箱与休息界面；点击音效 |

**仍未完成**（功能层面）：战败分支的奖励结算、卡牌升级的正式获取入口与 UI、商店与金币的完整闭环、战斗 / 反馈层音效、格挡与治疗意图的数值显示。内部技术债与架构评审结论不在此列，见 [架构分析报告](Doc/架构分析报告.md)。

---

## 当前进度与后续计划

**近期进展（2026-11 中旬）**
- 架构评审综合评分由 95 上调至 **96/100（A）**：完成「整局命令通道」重构——新增统一门面 `RunCommandDispatcher` 与幂等领奖会话 `RewardClaimService`，原跨层一致性缺口 K1（奖励面板直写域）/ K2（战斗外药水与计时直写域）已收口，表现层现在只经命令门面上报意图。
- 内容扩充：新增敌人 `ShrinkerBeetle`（含阶段二意图切换）、缩小 / 滑溜状态、灰水 / 甲虫汁药水、消耗 / 升级卡牌动作，均走既有数据驱动通道。

**后续计划**
1. 功能补全：战败分支奖励结算、卡牌升级正式获取入口与 UI、商店金币完整闭环、战斗 / 反馈层 `BE_*` 音效接入、格挡 / 治疗意图数值显示。
2. 技术债收口：D2（存档根键 `GetType().FullName` 静默丢档）、D4（`BattleEventDispatcher` 缺 `Unregister`）、K3（`RaiseMaxHp` 绕过 `RunState` 入口）、L-1（`UI_OneShotVfx` 魔法字符串）。
3. 架构演进：`Presentation` 已 11,926 行、逼近 12k 拆分触发线，评估首拆（按玩法子系统划分子 asmdef）；数值平衡与内容填量随纵向切片试玩推进。

---

## 环境与技术栈

| 项 | 值 |
| --- | --- |
| Unity | `2022.3.62f3c1`（LTS 国内版 c1） |
| 渲染管线 | Built-in |
| Git LFS | **必需**（`.gitattributes` 已将美术 / 音频 / 动画等二进制资源纳入 LFS） |
| IDE | JetBrains Rider（推荐）/ Visual Studio 2022 |

> IDE 只做 IntelliSense、重构与调试。**编译权永远归 Unity**——不要在 IDE 里 Build Solution 或 NuGet Restore；用 File > Open 打开项目根目录，不要打开 `.sln`。

| 类别 | 选型 |
| --- | --- |
| 依赖注入 | VContainer `1.19.0` |
| 异步 | UniTask |
| 补间动画 | DOTween（Demigiant） |
| 2D 骨骼动画 | Spine（spine-unity） |
| UI | uGUI `1.0.0` + TextMeshPro `3.0.9` |
| 输入 | Input System `1.14.2` |
| 序列化 | Newtonsoft.Json `3.2.2` |
| 多态配置 | MackySoft.SerializeReferenceExtensions（`[SerializeReference]` + `[SubclassSelector]`，源码内置于 `Assets/Plugins/`） |
| 编辑器特性 | NaughtyAttributes（仅编辑器绘制，运行时不参与） |
| 随包启用 | Timeline `1.7.7`、Visual Scripting `1.9.11`（非核心依赖） |

---

## 代码放在哪

| 路径 | 内容 |
| --- | --- |
| `Assets/_Scripts/` | 项目自有 C# 代码，按 8 个域分目录，每域一个 `asmdef` |
| `Assets/_Data/` | 策划可编辑的 `SO_*` 配置资产（卡牌 / 敌人 / 遗物 / 药水 / 事件 / 地图 / 存档目录等） |
| `Assets/_Art/`、`Assets/_Prefabs/` | 美术、音频、字体、动画与预制体 |
| `Assets/Scenes/` | 8 个场景（主菜单 + 整局层 + 六阶段） |
| `Assets/Plugins/`、`Assets/Editor/` | 第三方插件与编辑器工具集（含 ToolBoxHub、开发面板） |
| `Assets/Resources/` | 需运行时 `Resources.Load` 的少量资产（`SO_SaveCatalog`、DOTween 设置） |
| `Doc/` | 设定、系统设计与架构文档（见下节索引） |

八个域各自负责什么：

| 域 | 职责 |
| --- | --- |
| `Utility` | 纯 C# 通用件：状态机、确定性随机、可修正数值、对象池 |
| `ConfigData` | 配置资产定义（只有「做什么、做多少」，零执行逻辑） |
| `RuntimeData` | 运行时对象：`Creature` / `Player` / `CardModel` / 牌堆 / 状态 / 奖励 / 存档模型 |
| `EngineUtility` | 引擎级通用件，以及输入 / 场景 / 音频 / 存档四类 `_GameSystem` 子系统 |
| `GameBattle` | 战斗规则：回合状态机、动作系统、`Hook`，命令 / 事件 / 查询通道 |
| `GameRun` | 整局领域层：`RunState`、地图生成、六段 `Loop`/`Session`、遗物效果与抽取 |
| `CommonPresentation` | 跨阶段复用的表现件（卡面、悬停提示、飘字、奖励面板等） |
| `Presentation` | 各阶段专属 UI、场景装配与整局层（顶栏 / 地图 / 主菜单流程） |

---

## 架构一瞥

四句话概括这个工程的设计取向，逐文件评审另见架构报告：

- 8 个 `asmdef` 构成一张无环依赖 DAG，依赖方向由编译期强制；`CommonPresentation` 与 `Presentation` 是平级表现层，后者依赖前者为有意设计。
- 配置只读一次即烘焙为运行时对象，运行时状态不持有配置引用；规则与状态只存在一处。
- 表现层与域层之间固定四通道：**命令上行 / 事件下行 / 只读查询 / `Hook` 状态介入**；命令侧进一步分层为「段级 `XxxCommandDispatcher` + 整局级 `RunCommandDispatcher`」两级门面，常驻界面只上发意图、写域全部落在 `GameRun` 域（含幂等的 `RewardClaimService` 领奖会话）。预览与结算共用同一份规则，卡面数字与实际扣血在结构上不会不一致。
- 整局进度由 `RunState` 持有，阶段逻辑归 `GameRun` 域，UI 只上发意图、订阅事实；存档收敛到三处检查点。

→ 逐文件评审、依赖图与技术债清单见 **[Doc/架构分析报告.md](Doc/架构分析报告.md)**（综合评分 96/100，A）与 [Doc/asmdef-dependency-graph.svg](Doc/asmdef-dependency-graph.svg)。

---

## 文档索引

| 主题 | 文档 |
| --- | --- |
| 世界观与叙事 | [世界观与背景设定](Doc/世界观与背景设定.md)、[_角色台词总览](Doc/_角色台词总览.md) |
| 架构 | [架构分析报告](Doc/架构分析报告.md)、[程序集依赖图](Doc/asmdef-dependency-graph.svg) |
| 卡牌与敌人 | [_铁甲战士卡牌总览](Doc/_铁甲战士卡牌总览.md)、[_敌人设计总览](Doc/_敌人设计总览.md) |
| 跑图与阶段 | [_地图生成设计总览](Doc/_地图生成设计总览.md)、[_商店系统总览](Doc/_商店系统总览.md)、[_事件系统总览](Doc/_事件系统总览.md)、[事件设计·首批 13 个](Doc/事件设计_首批13个.md) |
| 养成与系统 | [_遗物系统总览](Doc/_遗物系统总览.md)、[_遗物可实现清单](Doc/_遗物可实现清单.md)、[_药水系统总览](Doc/_药水系统总览.md)、[_存档系统总览](Doc/_存档系统总览.md) |

---

## 开发约定

- **新增玩法代码一律放 `Assets/_Scripts/<域>/` 对应位置**，不得在 `Assets/` 根目录或其他目录散落 `.cs`（否则会落进 `Assembly-CSharp` 而脱离 `asmdef` 边界）。每个域一个 asmdef，依赖不得成环。
- **写新代码前记住三条**：状态变更只经动作系统；表现层不持业务规则、不直接篡改域状态；新增数值判定不要另写一份，与预览共用 `Hook.Resolve`。完整 10 条不变量见架构报告 §8。
- 提交前用 `git lfs ls-files` 确认本次新增的二进制资源已被 LFS 接管。
- 编码规范：private/protected 字段 `_camelCase`、枚举 `E` 前缀、UI 类 `UI_` 前缀、SO 类 `SO_` 前缀、public 方法用 `#region` 包裹、`using` 放 namespace 外。
- 注释写「为什么」和「踩过什么坑」，不写「做了什么」；**重构时不要删注释**——本项目的注释密度是其最有价值的部分。
