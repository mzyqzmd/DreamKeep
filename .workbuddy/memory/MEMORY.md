# DreamKeep 项目长期记忆

## 环境与工具链
- Unity `2022.3.62f3c1`（国内版 c1），安装路径 `D:\Unity\UnityEditor\2022_3_62f3c1`；本机另装 Unity 6 `60000_5_9f1`（勿误选）。
- 项目路径 `D:\Unity\Projects\DreamKeep`；Git 仓库 `mzyqzmd/DreamKeep`，已启用 LFS（348 个对象），`.gitignore` 已覆盖 `.idea/` `.vs/` `*.csproj` `*.sln`。
- IDE：2026-09 起从 VS2022 迁移至 **JetBrains Rider**（`com.unity.ide.rider 3.0.36` 已装）。迁移手册见 `Docs/05-迁移至JetBrains Rider.md`。
- 核心原则：**编译权永远归 Unity**，Rider 只做 IntelliSense/重构/调试；禁止在 Rider 里 Build Solution 或 NuGet Restore；必须用 File > Open 打开项目根目录而非 sln。

## 架构约定
- 程序集定义位于 `Assets/_Scripts/<域>/_DreamKeep.<域>.asmdef`，当前有效：Utility、RuntimeData、Data、Gameplay、Presentation（Presentation 目录仍为空，Unity 不会为其生成 csproj，属正常）。
- 依赖：VContainer 1.19.0（DI）、UniTask（异步）、Odin Inspector/Validator（编辑器扩展）、DOTween、Spine、Newtonsoft.Json 3.2.2。
- 编码规范（README 声明）：private/protected 字段 `_camelCase`、枚举 `E` 前缀、UI 类 `UI_` 前缀、SO 类 `SO_` 前缀、public 方法用 `#region` 包裹、using 放 namespace 外。

## 已知待办 / 技术债
- 根目录残留 9 个失效 csproj + `dk_compile.log` + `nul)` 待清理。
- 缺 `.editorconfig`，代码风格未机器化。
- README 架构描述与实际 asmdef 不一致，待更新。
- 行尾约定未统一（建议 Unity 侧改为 Unix LF）。
