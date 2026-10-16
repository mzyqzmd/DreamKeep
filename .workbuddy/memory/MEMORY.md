# DreamKeep 项目长期记忆

## 环境与工具链
- Unity `2022.3.62f3c1`（国内版 c1），安装路径 `D:\Unity\UnityEditor\2022_3_62f3c1`；本机另装 Unity 6 `60000_5_9f1`（勿误选）。
- 项目路径 `D:\Unity\Projects\DreamKeep`；Git 仓库 `mzyqzmd/DreamKeep`，已启用 LFS（348 个对象），`.gitignore` 已覆盖 `.idea/` `.vs/` `*.csproj` `*.sln`。
- IDE：2026-09 起从 VS2022 迁移至 **JetBrains Rider**（`com.unity.ide.rider 3.0.36` 已装）。迁移手册见 `.codebuddy/Docs/05-迁移至JetBrains Rider.md`。
- 核心原则：**编译权永远归 Unity**，Rider 只做 IntelliSense/重构/调试；禁止在 Rider 里 Build Solution 或 NuGet Restore；必须用 File > Open 打开项目根目录而非 sln。

## 架构约定
- 程序集定义位于 `Assets/_Scripts/<域>/_DreamKeep.<域>.asmdef`，当前有效：**Utility、ConfigData、RuntimeData、GameBattle、Presentation**；依赖为无环 DAG（非单链）：`ConfigData` 与 `Utility` 同为叶层（`ConfigData` 不依赖 `Utility`，仅依赖外部包），`RuntimeData → {Utility, ConfigData}`，`GameBattle → {Utility, RuntimeData, ConfigData}`，`Presentation → 全部四个`（`BattleContext`/`BattleSetup` 在 `GameBattle/Battle/`，命名空间 `_DreamKeep.GameBattle`）。
- 架构边界原则（2026-09-14 咨询共识，详见 Docs/04 §0.5）：程序集数量＝需强制的依赖方向数，**不按玩法系统开程序集**——未来跑图＝一个域（`GameRun`，第一个跑图系统开写时再建，方向 `GameRun → GameBattle`，战斗不认识跑图）；桥（PresentationBridge）跟域走、各域自持，不建统一桥接程序集，机制等第二个消费者再抽；编排角色必要但不单独成集（`GameRun` 内 `RunController`）；音效/粒子代码归 `Presentation/Feedback/`，触发走 `BE_*` 订阅。
- 依赖：VContainer 1.19.0（DI）、UniTask（异步）、NaughtyAttributes（`[ShowIf]` 等编辑器特性，仅编辑器绘制、运行时不参与）+ MackySoft.SerializeReferenceExtensions（`[SerializeReference]`+`[SubclassSelector]` 多态配置，以源码置于 `Assets/Plugins/`，非 manifest 包）、DOTween、Spine、Newtonsoft.Json 3.2.2。**Odin Inspector/Validator 从未被本项目实际引用**（manifest/源码/条件编译全无），仅 Player Settings 的 Scripting Define Symbols 残留 `ODIN_INSPECTOR`/`ODIN_VALIDATOR` 死宏，待清理。
- 编码规范（README 声明）：private/protected 字段 `_camelCase`、枚举 `E` 前缀、UI 类 `UI_` 前缀、SO 类 `SO_` 前缀、public 方法用 `#region` 包裹、using 放 namespace 外。

## 架构现状（2026-10-14 逐文件审阅核验）
- 规模：`Assets/_Scripts` 共 176 个 .cs / 7400 行（Utility 12·ConfigData 32·RuntimeData 28·GameBattle 65·Presentation 39）；169/176 文件 <150 行，超出 7 个均为表现层内聚单体。
- 五个 asmdef 的 GUID 引用已逐条反解核验：**无环、无反向引用**；`GameBattle → ConfigData` 必要（BattleSetup 装配 + BP_Burn/BP_Regen 类型判定）。
- 三段式通信已全线接通（推翻 2026-09-14 的"⑤ 战斗桥未接线"）：上行 `BattleCommandDispatcher → BattleLoop.EnqueueCommand`，下行 `BattleAction.Execute → List<IBattleEvent> → BattleEventDispatcher.PlayAsync`；25 个 `BE_*` 全为 struct 只读载荷。
- 反模式扫描近乎全零：0 处 `GameObject.Find`/`FindObjectOfType`/`Resources.Load`/`InvokeRepeating`/单例/`DontDestroyOnLoad`；全项目仅 1 处场景级查找 `FindFirstObjectByType<UI_PopTextRoot>()`（`UI_HoverTip.cs`，懒查+伪空重查，建议改 `[Inject]`）；3 处 `Update()`（`UI_HandView`/`UI_CardBattle`/`UI_TargetDirector`），均在表现层且只读域侧。
- 已形成并需沿用的约定（详见根目录 `架构分析报告.md` §6 完整 10 条）：状态变更只经动作系统、命令只入队不执行、失败路径也走事件（`BE_CardPlayRejected`）、动作间用"幂等跳过"协作、配置只读一次、模板/实例同类型必须显式 Clone（`CA_Grant` 递归克隆）、组合根是唯一同时认识配置与运行时对象的地方。
- 架构评分 91/100（A），报告在根目录 `架构分析报告.md`（2026-10-15 复核，相对 2026-10-14 的 88 因手牌区重构收口、通用对象池与可复用说明气泡抽取而上调）。

## 已知待办 / 技术债
- ~~`UI_HandView` 手牌区重构未收口~~ **已于 2026-10-15 收口**：本体由 841 行拆出 `UI_HandHover.cs`(231，悬停裁决/避让/词条气泡对账) 与 `UI_HandShow.cs`(222，11 条手牌事件演出) 后降为 493 行编排器，旧重复方法已清除，三组件同节点经 `RequireComponent`+`internal` 服务面协作，无双重演出。SRP 维度已 +3 分。
- **近期重构/抽取（2026-10-13~15）**：通用对象池 `MonoObjectPool<T>` 抽至 `Utility/Engine/Pool`（卡牌飞行复用）；说明气泡发起器 `UI_HoverTip` 抽成可复用组件（Pointer/External 双模式、静态/动态双内容）；修复手牌悬停漂移、抽牌入场数字弹跳、悬停对齐指针、拖拽阻尼跟随等手感问题。
- **功能空洞（非死代码）**：`BP_Strength`/`BP_Weak`/`BP_Vulnerable` 有配置/图标/描述/`Clone` 但全项目零消费——`BA_Damage` 直接按 `BaseAmount` 结算不查修正，玩家拿到这三个状态当前无任何效果。`ModifiableStat.Add`/`Remove` 同样零调用。
- 其他缺口：卡牌升级（`CM_Runtime.Upgrade()` 零调用）、战斗结束奖励（`BP_BattleFinish.HandleCommand` 空实现）、商店金币（`SO_Player._startGold` 零消费）、格挡/治疗意图图标不显示数值、敌方不结算自身灼烧/再生。
- `BattleEventDispatcher` 无 `Unregister`，单位视图销毁后处理器仍留字典（当前靠表现层 null 检查兜住，无实际 bug，但每死一个敌人泄漏一组闭包）。
- 根目录残留 151 个 csproj（多数为第三方包生成的，未被 `.gitignore` 的 `*.csproj` 规则命中——需确认是否因已被 track 而规则失效）。
- 缺 `.editorconfig`，代码风格未机器化。行尾约定未统一（建议 Unity 侧改为 Unix LF）。
- 程序集小口（2026-09-14 盘点，暂定不修）：① 五个 asmdef 的 `overrideReferences` 均 false（预编译 DLL 仍注入）；② `Presentation.asmdef` 引用了未直接使用的 `Unity.Mathematics`；③ `Assets/_Data/Slime.asset` 6 处 `[SerializeReference]` 因改名失效（需重挂或加 `[MovedFrom]`）。
- ~~`ProjectSettings` 残留 Odin 死宏~~ **已于 2026-10-14 清除**：Standalone 平台 Scripting Define Symbols 中的 `ODIN_INSPECTOR`/`ODIN_VALIDATOR` 死宏已删除；项目现无任何 Odin 包/源码/宏痕迹，依赖记录已与代码现实对齐。
