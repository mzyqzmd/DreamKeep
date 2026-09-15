# DreamKeep 项目长期记忆

## 环境与工具链
- Unity `2022.3.62f3c1`（国内版 c1），安装路径 `D:\Unity\UnityEditor\2022_3_62f3c1`；本机另装 Unity 6 `60000_5_9f1`（勿误选）。
- 项目路径 `D:\Unity\Projects\DreamKeep`；Git 仓库 `mzyqzmd/DreamKeep`，已启用 LFS（348 个对象），`.gitignore` 已覆盖 `.idea/` `.vs/` `*.csproj` `*.sln`。
- IDE：2026-09 起从 VS2022 迁移至 **JetBrains Rider**（`com.unity.ide.rider 3.0.36` 已装）。迁移手册见 `.codebuddy/Docs/05-迁移至JetBrains Rider.md`。
- 核心原则：**编译权永远归 Unity**，Rider 只做 IntelliSense/重构/调试；禁止在 Rider 里 Build Solution 或 NuGet Restore；必须用 File > Open 打开项目根目录而非 sln。

## 架构约定
- 程序集定义位于 `Assets/_Scripts/<域>/_DreamKeep.<域>.asmdef`，当前有效（2026-09-14 同步）：**Utility、ConfigData、RuntimeData、GameBattle、Presentation**；依赖链 `Utility ← ConfigData ← RuntimeData ← GameBattle ← Presentation`（`BattleContext`/`BattleSetup` 在 `GameBattle/Battle/`，命名空间 `_DreamKeep.GameBattle`）。
- 架构边界原则（2026-09-14 咨询共识，详见 Docs/04 §0.5）：程序集数量＝需强制的依赖方向数，**不按玩法系统开程序集**——未来跑图＝一个域（`GameRun`，第一个跑图系统开写时再建，方向 `GameRun → GameBattle`，战斗不认识跑图）；桥（PresentationBridge）跟域走、各域自持，不建统一桥接程序集，机制等第二个消费者再抽；编排角色必要但不单独成集（`GameRun` 内 `RunController`）；音效/粒子代码归 `Presentation/Feedback/`，触发走 `BE_*` 订阅。
- 依赖：VContainer 1.19.0（DI）、UniTask（异步）、Odin Inspector/Validator（编辑器扩展）、DOTween、Spine、Newtonsoft.Json 3.2.2。
- 编码规范（README 声明）：private/protected 字段 `_camelCase`、枚举 `E` 前缀、UI 类 `UI_` 前缀、SO 类 `SO_` 前缀、public 方法用 `#region` 包裹、using 放 namespace 外。

## 已知待办 / 技术债
- 根目录残留 9 个失效 csproj + `dk_compile.log` + `nul)` 待清理。
- 缺 `.editorconfig`，代码风格未机器化。
- 行尾约定未统一（建议 Unity 侧改为 Unix LF）。
- 程序集小口（2026-09-14 盘点，详见 Docs/04 §0.6，暂定不修）：① `ConfigData`/`Presentation` 各一条指向 `Utility` 的空引用；② `Utility.asmdef` 的 `overrideReferences` 仍为 false（预编译 DLL 仍注入）；③ asmdef 文件名 `_DreamKeep.Data.asmdef` 未改名（内部名已是 `_DreamKeep.ConfigData`）；④ `Assets/_Data/Slime.asset` 6 处 `[SerializeReference]` 因改名失效（需重挂或加 `[MovedFrom]`）；⑤ 战斗桥未接线（`Presentation` 零 `Register<` 订阅）。
