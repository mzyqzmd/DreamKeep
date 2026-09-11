# DreamKeep · 迁移至 JetBrains Rider

> 适用对象：本机 DreamKeep 项目（`D:\Unity\Projects\DreamKeep`）
> 目标编辑器：Unity `2022.3.62f3c1` (LTS 国内版 c1)
> 编写日期：2026-09-09
> 文档性质：可执行迁移手册（含体检结论、操作步骤、验收清单、回滚方案）

---

## 0. 先给结论

当前项目**已经具备 90% 的 Rider 接入条件**——`com.unity.ide.rider 3.0.36` 包已安装、`.gitignore` 已覆盖 `.idea/`、`.NET SDK 9.0.315` 已就位、Git LFS 3.7.1 已配置。

迁移真正的工程量集中在三处：

1. **本机未安装任何 JetBrains 产品**（`%LOCALAPPDATA%\JetBrains`、`C:\Program Files\JetBrains` 均为空）→ 需先安装 Rider。
2. **Unity 侧尚未切换外部脚本编辑器**，且一行尾与 csproj 生成范围需要校准。
3. **工作区存在未提交的重构改动**（大量重命名 + 删除）→ 必须先固化一个可回滚锚点再动 IDE。

其余均为低风险的配置项迁移，不涉及任何代码改动。

---

## 1. 体检结论（迁移前基线）

### 1.1 环境快照

| 项 | 实测值 | 迁移影响 |
| --- | --- | --- |
| Unity 版本 | `2022.3.62f3c1`（国内版 c1 后缀），路径 `D:\Unity\UnityEditor\2022_3_62f3c1` | Rider 需经 Unity Hub 的 `secondaryInstallPath.json` 发现该路径；该文件已存在且指向正确位置 ✅ |
| Unity 6 | 另装有 `60000_5_9f1` | Rider 会一并列出，注意不要误选为本项目编辑器 |
| Rider 集成包 | `com.unity.ide.rider 3.0.36` ✅ | 无需安装，版本足够；勿降级 |
| VS 集成包 | `com.unity.ide.visualstudio 2.0.22` | 与 Rider 包可共存；彻底切 Rider 后可移除（见 §9.4） |
| 脚本后段 | `LangVersion 9.0` / `net472` (TargetFrameworkVersion v4.7.1) / `API Compatibility Level 6 (.NET Standard 2.1)` | Rider 用自带 MSBuild 解析即可，**不要**改 LangVersion |
| 预定义符号 | `ODIN_INSPECTOR`、`ODIN_VALIDATOR`、`DOTWEEN`、`UNITY_POST_PROCESSING_STACK_V2` 等 200+ 个 | 由 Unity 写入 csproj，Rider 自动读取，无需手工配置 |
| .NET SDK | `9.0.315`（`C:\Program Files\dotnet\sdk`） | Rider 运行与 MSBuild 解析均不强制依赖它，可保留 |
| Git | 仓库 `mzyqzmd/DreamKeep` + LFS（`git-lfs/3.7.1`，348 个 LFS 文件） | Rider 调用本机 git.exe，LFS 透明生效 |
| 编辑器缓存残留 | `.vs/`、`.vsconfig`（ManagedGame workload）、11 个历史 csproj、`dk_compile.log`(51 KB)、`nul)`(24 B) | 见 §9 清理清单 |

### 1.2 依赖清单（迁移后必须仍可跳转源码）

| 依赖 | 来源形态 | Rider 识别方式 |
| --- | --- | --- |
| UniTask | Git UPM（`Packages/manifest.json`） | `Library/PackageCache`，需开启 Git 包的项目生成 |
| VContainer `1.19.0` | Git UPM | 同上 |
| Newtonsoft.Json `3.2.2` | Registry UPM | 同上 |
| Odin Inspector / Validator `3.x` | `Assets/Plugins/Sirenix`（非 UPM） | 随 `Assembly-CSharp-firstpass` 纳入 |
| DOTween | `Assets/Plugins/Demigiant` | 同上 |
| Spine | `Assets/Plugins/Spine`（3 个 asmdef） | 独立 asmdef，已进 sln |
| UniTask / Timeline / Visual Scripting / Test Framework | UPM | 已进 sln |

### 1.3 当前解决方案构成（`DreamKeep.sln`，共 10 个项目）

`Assembly-CSharp-Editor`、`Assembly-CSharp-firstpass`、`_DreamKeep.Utility`、`_DreamKeep.RuntimeData`、`_DreamKeep.Data`、`_DreamKeep.Gameplay`、`Sirenix.OdinInspector.Modules.UnityMathematics`、`spine-csharp`、`spine-unity`、`spine-unity-editor`。

> **注意（容易误判的一点）**：`Assets/_Scripts/Presentation/_DreamKeep.Presentation.asmdef` 存在，但 sln 中**没有**它。这不是丢失，而是该目录下**尚无任何 `.cs` 文件**，Unity 对空程序集不生成 csproj。一旦写入首个脚本，Unity 重新生成解决方案后会自动出现。迁移验收时勿据此判定失败。

### 1.4 迁移前置风险（必须先处理）

`git status` 显示工作区存在未提交改动：多条 `R`（`Action_*` → `IBA_*` 重命名）、多条 `D`（`ActionSystem.cs`、`AffixActionFactory.cs` 等删除）。

**原因**：切换 IDE 会触发 Unity 重新生成 csproj/sln 并部分重建 `Library/`，若此时首次索引异常，容易与既有改动混淆，且失去干净的回滚点。

```bash
# 方案 A（推荐）：先固化当前成果
git add -A && git commit -m "refactor: 行动层 Action_* 重命名为 IBA_*（Rider 迁移前锚点）"

# 方案 B：暂不提交，先冻结
git stash push -u -m "pre-rider-migration"
```

---

## 2. 阶段一：安装 Rider（本机尚未安装）

1. 下载：**JetBrains Toolbox App** 为首选安装方式（便于后续版本管理与回滚），或直接下载 Rider 安装包。
2. 版本选择：选择 **Rider 2025.1 或更新**。Unity 2022.3 在 Rider 支持矩阵内；若同时要用 Unity 6 项目，版本越高越好。
3. 安装时勾选：
   - ✅ 关联 `.sln` / `.csproj`（方便双击应急打开）
   - ✅ 添加 `rider` 到 PATH（命令行 `rider .` 打开项目）
4. 首次启动：**不要**在欢迎界面选 “Open” 打开任何东西，先装插件（见 §8）后再按 §3 的方式打开项目。

> Rider 自带 bundled MSBuild 与运行时，**不依赖本机 .NET SDK**。已装的 `9.0.315` 保留无害，但请勿将其指定为 Unity 项目的编译工具链。

---

## 3. 阶段二：Unity 侧接入（最关键的一步）

> **顺序不可颠倒**：先配 Unity，再由 Unity 生成解决方案，最后用 Rider 打开。反过来会出现引用缺失。

### 3.1 切换外部脚本编辑器

Unity Editor → `Edit > Preferences > External Tools`（macOS 为 Preferences）：

| 设置项 | 目标值 | 说明 |
| --- | --- | --- |
| External Script Editor | **JetBrains Rider** | 列中若显示 `Rider <version>`，说明 `com.unity.ide.rider` 已被识别 |
| External Script Editor Args | 保持默认 | 默认含 `"$(File)"` 及行号参数，勿删 |
| **Editor Attaching** | ✅ **勾选** | **不开此项无法调试**。Unity 2022 默认开启，切换后请复核 |
| Generate .csproj files for | 全勾（Registry / Built-in / **Git** / Local / **Player projects**） | Git 包不勾会导致 UniTask、VContainer 源码无法跳转 |
| OSC / Analysis | 保持默认 | — |

### 3.2 重新生成解决方案

`Assets > Open C# Project`（或上述 Preferences 切换后 Unity 自动触发一次）。生成后确认：

- 根目录下 `DreamKeep.sln` 时间戳刷新；
- 仍为 10 个项目（见 §1.3），无重复、无缺失；
- 不应新增 `_DreamKeep.Core / Domain / Application / Composition / Simulation` 等项目——它们对应的 asmdef 早已不存在（见 §9.2）。

### 3.3 校验 Unity 编辑器路径发现

Rider 通过 Unity Hub 的 `%APPDATA%\UnityHub\secondaryInstallPath.json` 发现非标准路径。本机该文件内容实测为：

```
"D:\\Unity\\UnityEditor\\2022_3_62f3c1"
```

✅ 指向正确。若 Rider 首启仍报 “Unity Editor not found”，在 `Settings > Languages & Frameworks > Unity Engine` 手动 `Add custom Unity Editor` 指定该路径。

---

## 4. 阶段三：SDK 与依赖项配置

### 4.1 明确一条原则

**Unity 项目的编译永远由 Unity Editor 完成，Rider 不负责编译，也不负责 NuGet Restore。**

Rider 在这里的角色是：读取 Unity 生成的 csproj → 提供 IntelliSense / 导航 / 重构 / 静态分析 → 通过调试协议附加到 Unity 进程。

因此：

| 操作 | 是否要做 |
| --- | --- |
| 在 Rider 里 `Build Solution` / `Ctrl+Shift+B` | ❌ **禁止**。会在项目目录产生 `bin/obj` 垃圾，且引用不全必然报错 |
| Restore NuGet Packages | ❌ 关闭自动 Restore。Unity csproj 无 `PackageReference` |
| 手动编辑 csproj / sln | ❌ 禁止。Unity 每次进入 Play 会覆盖，改动必丢 |
| 在 Unity 里改 asmdef 后让 Rider 同步 | ✅ 唯一正道 |

### 4.2 Rider 内 MSBuild 设置

`Settings > Build, Execution, Deployment > Toolset and Build`：

- MSBuild version：**JetBrains bundled MSBuild**（默认，不要改成 dotnet CLI）；
- 取消勾选 `Automatically restore NuGet packages`；
- `Solution-wide analysis`：建议设为 `Solution must be loaded` 关闭（Unity 项目体量大，常驻分析会明显吃 CPU）。

### 4.3 依赖源码跳转校验

Ctrl+Click 依次验证以下符号，均应跳到 `Library/PackageCache` 下真实源码而非元数据：

- `UniTask` → `Cysharp.Threading.Tasks`
- `VContainer.BeginLifetimeScope` → `jp.hadashikick.vcontainer@...`
- `[SerializeField]` + Odin 特性 → `Assets/Plugins/Sirenix`

若某包只显示元数据、进不了源码，回到 §3.1 检查 “Generate .csproj files for > Git packages”。

---

## 5. 阶段四：Rider 侧项目打开与基础配置

### 5.1 打开方式（重要）

用 **File > Open > 选择项目根目录 `D:\Unity\Projects\DreamKeep`**，而不是双击 `DreamKeep.sln`。

- 打开目录：Rider 识别为 **Unity-generated project**，自动禁用构建、启用 Unity 面板、使用 Unity 的编译结果作为语义来源；
- 打开 sln：退化为普通 .NET 解决方案，Unity 集成面板消失，双击 Build 会坏事。

> 已关联的 `.sln` 双击仍可应急，但日常请始终用目录模式。

### 5.2 必须校准的设置

| 路径 | 设置 | 值 |
| --- | --- | --- |
| `Settings > Languages & Frameworks > Unity Engine` | Enable Unity integration | ✅ |
| 同上 | Unity Editor | 自动检测到 `2022.3.62f3c1` |
| `Settings > Editor > Code Style > C#` | Line endings | **LF** |
| `Settings > Editor > General > Editor Tabs` | 关闭 `Strip trailing blank lines`? | 建议保持默认；关键是只读 Unity `.meta` 时不格式化 |
| `Settings > Version Control > Git` | Git executable | 指向本机 `git.exe`（含 LFS 的那个） |
| View > Tool Windows | 打开 Unity / Unity Tests / TODO | 常驻左侧 |

### 5.3 建议补一份 `.editorconfig`（当前项目根目录**没有**）

现状：项目 README 声明了编码约定（`private/protected` 字段 `_camelCase`、枚举 `E` 前缀、`UI_` / `SO_` 前缀、`#region` 包裹 public 方法、方法参数 `_` 尾下划线），但缺乏机器可读的规则文件。Unity 与 Rider 各自格式化时容易打架。

落地建议：在项目根新建 `.editorconfig`（UTF-8 + LF），至少锁住：

```editorconfig
root = true

[*.cs]
end_of_line = lf
charset = utf-8
indent_style = space
indent_size = 4
insert_final_newline = true

dotnet_naming_rule.private_field_camel_underscore.severity = warning
dotnet_naming_rule.private_field_camel_underscore.symbols = private_fields
dotnet_naming_rule.private_field_camel_underscore.style = underscore_camel
dotnet_naming_symbols.private_fields.applicable_kinds = field
dotnet_naming_symbols.private_fields.applicable_accessibilities = private, protected
dotnet_naming_style.underscore_camel.capitalization = camel_case
dotnet_naming_style.underscore_camel.required_prefix = _

csharp_new_line_before_open_brace = all
csharp_using_directive_placement = outside_namespace:silent   # using 放 namespace 外
```

Rider 会自动读取，随后可用 `Ctrl+Alt+L` 全平台保持同一套规则。

---

## 6. 阶段五：构建与运行任务的迁移

### 6.1 编译：从 IDE 迁移到 Unity

| 原 VS/VSCode 动作 | Rider 中的等价动作 |
| --- | --- |
| VS 里 Build | 切到 Unity Editor，或 `Ctrl+R`（Rider 的 **Refresh Unity solution**，等价于切回 Unity 触发编译） |
| VS 错误列表 | Unity Console（Rider 内也有 Unity 面板同步显示编译日志） |
| 编译含 Roslyn analyzer | Unity 负责，改 asmdef 控制 |
| unsafe 代码 | 在对应 asmdef 的 `allowUnsafeCode` 打开，Rider 无需额外设置 |

### 6.2 运行 / 调试配置

`Run > Edit Configurations… > + > Unity Editor`：

- **Name**：`Unity Editor (DreamKeep)`
- 该配置行为：Attach 调试器到 Unity Editor，或直接 **Attach & Play**（启动 Play Mode 并自动 attach）。

建议同时建第二条：**Unity Player**（用于 Standalone 构建后的外部进程调试）。README 声明目标平台为 PC；后续若打 Windows 包，必须在 Build Settings 中勾选 `Development Build` + `Script Debugging`，否则构建出的 Player 不接受调试器附着。

### 6.3 Play Mode 与 Domain Reload 的交互（本项目实测相关）

`ProjectSettings/EditorSettings.asset` 中 `m_EnterPlayModeOptionsEnabled: 0`——即**未启用** “跳过 Domain Reload”。这意味着：

- 每次进入 Play 都会发生 Domain Reload，**已附加的调试会话会被中断**，需在 Play 之后再 attach 一次；
- Rider 的 “Attach to Unity Editor & Play” 配置会自动处理这个时序（先 Play，domain reload 完成后自动接上），建议直接使用该配置而非手动点 Play。

若后续为缩短迭代时间开启 `Enter Play Mode Options (Disable Domain Reload)`，调试体验会更稳定（断点跨 Play 不丢），但代价是需要自行处理静态状态重置——这与 README 中 “Core 硬隔离、状态只存在一处” 的架构约束直接耦合，属于独立决策，不在本次迁移范围。

---

## 7. 阶段六：版本控制集成

### 7.1 Git

1. `Settings > Version Control > Git`：确认 executable 为本机 git（实测 `git-lfs/3.7.1` 与其同源）；
2. 不要启用 `Perform update branches automatically` 之外的自动 pull/rebase 选项，避免打断 Unity 正在进行的资产导入；
3. 提交入口：`Ctrl+K`（Rider Commit 工具窗口）；变更按 Changelist 分组，可把 “AI 辅助改动” 单独放一个变化列表。

### 7.2 Git LFS

- Rider 调用 native git → LFS filter 自动生效，**无需插件**；
- `git lfs ls-files` 实测 348 个对象已接管；
- **注意**：Rider 的 diff 工具只能读文本。`.prefab` / `.unity` 这类 text 型 LFS 文件可 diff；`*.png` 等 binary 型请勿双击走内部 diff；
- 首次克隆别的机器仍需 `git lfs install && git lfs pull`（README 已注明，保持）。

### 7.3 行尾（存在一处不一致，建议统一）

| 配置 | 当前值 |
| --- | --- |
| `.gitattributes` | `* text=auto eol=lf` → 工作区强制写 **LF** |
| Git `core.autocrlf` | `true` |
| Unity `m_LineEndingsForNewScripts` | `0`（OS Native → 本机为 **CRLF**） |

分析：`.gitattributes` 的 `eol=lf` 优先级高于 `core.autocrlf`，入库会被归一化，所以**当前不会造成仓库污染**。但 Unity 新建脚本时会写入 CRLF，与仓库的 LF 约定不一致，`.editorconfig` 的 `end_of_line = lf` 也无法改变 Unity 新建文件时的行为。

建议（可选，一次性调整）：Unity Editor → `Project Settings > Editor > Line endings for new scripts` 设为 **Unix (LF)**，使三处约定收敛。该项写回 `ProjectSettings/EditorSettings.asset`，属团队可见改动，改前知会协作方。

### 7.4 `.idea/` 的处理

`.gitignore` 第 82 行已包含 `.idea/` ✅ —— JetBrains 用户级/项目级索引不会进仓库，这一步**已经到位，无需改动**。

补充建议：若希望团队共享同一套代码风格与检查规则，可在 `.gitignore` 中放行 `codeStyles` 与 `inspectionProfiles` 两个子目录（其余 `.idea/**` 仍忽略）：

```gitignore
.idea/**
!.idea/.idea.DreamKeep/.idea/codeStyles/
!.idea/.idea.DreamKeep/.idea/codeStyles/**
!.idea/.idea.DreamKeep/.idea/inspectionProfiles/
!.idea/.idea.DreamKeep/.idea/inspectionProfiles/**
```

（此项为可选增强，非迁移必需。）

---

## 8. 阶段七：原开发环境工具替换对照

本项目此前的使用痕迹：`.vs/` 缓存 + `.vsconfig`（VS2022 ManagedGame workload）+ `.vscode/` 已被忽略但目录不存在 + `.codebuddy/`（已忽略，本地保留）。

| 原工具 / 插件 | 替代方案 | 位置 |
| --- | --- | --- |
| **Visual Studio 2022 + ManagedGame workload**（`.vsconfig`） | 不再需要。Rider 自带 Unity 支持，缺少 VS 也不影响编译 | 可删除 `.vsconfig`（见 §9.3） |
| **Unity Visual Studio Tools**（解决方案生成 / 调试 / 代码生成） | Rider 内置 Unity Engine 集成 + `com.unity.ide.rider` | 内置 |
| **ReSharper（VS 付费插件）** | Rider 内置同一套引擎，无需另装 | 内置 |
| **CodeBuddy VS2022 插件 / 通义灵码（`.lingma`）** | JetBrains Marketplace 内搜对方的 JetBrains 版插件并安装（两者均有 JetBrains 官方发行版），装完在 `Settings > Tools` 下按其文档配置 | Marketplace |
| **VS Code C# Dev Kit / Omnisharp** | 不需要，Rider 自带 Roslyn 级解析 | — |
| **VS Test Explorer / VS Code Unity Test Explorer** | Rider **Unity Tests** 工具窗口，直接跑 EditMode / PlayMode | View > Tool Windows |
| **ShaderlabVS / Shader 语法高亮插件** | Rider 内置 ShaderLab / HLSL 高亮与分析 | 内置 |
| **GitLens** | Rider 内置 Git + Annotate；增强可装 **GitToolBox** 插件 | Marketplace |
| **Todo Tree** | Rider 内置 **TODO** 工具窗口（`Settings > Editor > TODO` 配置 pattern） | 内置 |
| **Roslyn Analyzers / 风格检查插件** | ReSharper Inspections + §5.3 的 `.editorconfig` | 内置 |

> Rider 插件安装路径：`Settings > Plugins > Marketplace`。建议保留 Unity 相关插件为默认版本，不要手动升级 bundled 插件。

---

## 9. 清理清单（本次体检发现的实际问题）

### 9.1 根目录两处临时残留

| 文件 | 大小 | 来源 | 处理 |
| --- | --- | --- | --- |
| `dk_compile.log` | 51,929 B | AI 会话中重定向出的编译日志 | **删除**（属工作区污染） |
| `nul)` | 24 B | Windows 下重定向到 `nul` 时误建的物理文件 | **删除**（文件名含 `)`，资源管理器难删，须用 shell） |

```bash
# 预览（不执行删除）
ls -l dk_compile.log 'nul)'

# 确认无误后执行
cd "D:/Unity/Projects/DreamKeep" && rm -f dk_compile.log 'nul)'
```

### 9.2 失效的 csproj 残留

根目录现有 20 个 csproj，`DreamKeep.sln` 只引用其中 10 个；其余 10 个为历史迭代遗留，均已不匹配任何现存 asmdef。它们统一被 `.gitignore` 的 `*.csproj` 规则覆盖，不会进仓库，但残留在磁盘会在 “双击 csproj 打开” 或全局搜索场景下造成重复类型解析。

**确定失效（9 个，对应 asmdef 已不存在）：**

```
Assembly-CSharp.csproj                    (2026-09-05)
NewAssembly.csproj                        (2026-09-05)
_DreamKeep.Application.csproj             (2026-09-05)
_DreamKeep.Core.csproj                    (2026-09-06)
_DreamKeep.Composition.csproj             (2026-09-07)
_DreamKeep.Domain.csproj                  (2026-09-07)
_DreamKeep.Simulation.csproj              (2026-09-05)
DOTween.Modules.csproj                    (2026-09-06)
DreamKeep.EditModeTests.csproj            (2026-09-06)
```

**暂未生成（1 个，asmdef 有效但目录下无 `.cs`，请勿删除）：**

```
_DreamKeep.Presentation.csproj            (2026-09-07)
```

另有三个 `.user` 文件（`Assembly-CSharp-Editor.csproj.user`、`_DreamKeep.Core.csproj.user`、`_DreamKeep.RuntimeData.csproj.user`）同属 VS 遗留，已被 `*.user` 忽略。

处理原则：**不要手动删任何一个 csproj**，正确做法是——

1. 确认对应的 asmdef 确实已删（`Assets/**/*.asmdef` 中已无 Application/Core/Composition/Domain/Simulation/DOTween.Modules/EditModeTests）；
2. Unity `Preferences > External Tools` 切换为 Rider，让 Unity **重新生成**解决方案；
3. Unity 只会重写它认识的项目，残留文件随后按下列 dry-run → 执行流程清理。

```bash
cd "D:/Unity/Projects/DreamKeep"

# Dry-run：列出“存在于磁盘但不在 DreamKeep.sln 中”的 csproj
comm -23 <(ls *.csproj | sort) \
         <(grep -oE '"[^"]+\.csproj"' DreamKeep.sln | tr -d '"' | sort)

# 预期输出 = 上述 10 个文件名
# 逐条核对无误后，仅 rm 上面“确定失效”的 9 个；勿用 *.csproj 通配符
```

> `_DreamKeep.Presentation.csproj` 必然出现在 comm 结果中，但它对应的 asmdef 有效 —— 保持不动，等该目录下出现首个 `.cs` 后由 Unity 重新生成。

### 9.3 VS2022 遗留

`.vs/`（已被 ignore）与 `.vsconfig`：`.vsconfig` 目前**已入版本库**，内容是 `Microsoft.VisualStudio.Workload.ManagedGame` 工作负载声明。切换到 Rider 后它不参与任何构建行为，保留无害，删除也不影响 Unity 侧编译。**建议留着不动**，直到确认长期不再使用 VS2022。

### 9.4 UPM 包取舍（可选）

`com.unity.ide.visualstudio 2.0.22` 与 Rider 包共存时，Unity 会额外生成 VS 专用产物并多一次文件中的手写记录。 **确认不再回到 VS2022 后**，可从 `Packages/manifest.json` 移除该行，再让 Unity 重新导入。风险等级低，但会影响所有协作者，建议作为迁移收尾动作、在团队同步后执行。

### 9.5 文档漂移（顺带发现，非阻塞）

`README.md` 的架构章节写的是 “四个程序集、依赖链 `Core ← Domain ← Data ← Presentation`”，与当前实际的 5 个 asmdef（`Utility` / `RuntimeData` / `Data` / `Gameplay` / `Presentation`，且 `Presentation` 为空）已不一致。README 的 IDE 一栏已含 Rider，迁移后建议顺手更新架构章节，避免误导新读者。

---

## 10. 验收清单（全部通过才算迁移完成）

按顺序复核，逐条打勾：

- [ ] Unity `Preferences > External Tools > External Script Editor` = **JetBrains Rider**
- [ ] Unity `Preferences > External Tools > Editor Attaching` = **勾选**
- [ ] `Generate .csproj files for` 含 **Git packages**
- [ ] Unity 双击任一 `.cs` → Rider 打开且光标落在正确符号
- [ ] Rider 右下状态栏 Unity 图标 → 显示已连接 `2022.3.62f3c1`（不是 Unity 6）
- [ ] Solution Explorer 显示 §1.3 列出的 **10 个**项目，无重复、无红色缺失引用
- [ ] Rider 无 ` Resolve to metadata` 警告；`#if ODIN_INSPECTOR` 分支正常高亮（符号同步成功）
- [ ] Ctrl+Click 可进入 **UniTask** / **VContainer** 源码
- [ ] 在 `_DreamKeep.Gameplay` 下的一个 `Update()` 打断点 → Attach to Unity Editor & Play → **断点命中**，可查看局部变量与调用栈
- [ ] Rider 内 `Ctrl+K` 完成一次提交，`git status` 干净；LFS 文件未被当成普通二进制提交
- [ ] Rider **Unity Tests** 窗口能列出 EditMode / PlayMode 用例并执行
- [ ] 全程未执行过 Build Solution / NuGet Restore；项目根**未出现** `bin/`、`obj/`（根目录原有的 `obj/` 是 Unity/VS 历史产物，已被 ignore）
- [ ] `git status` 干净（不含 `.idea/`、`*.csproj`、`dk_compile.log` 等）

---

## 11. 故障排查表

| 症状 | 根因 | 处置 |
| --- | --- | --- |
| 脚本右键 “Open C# Project” 打开的还是 VS | Unity Preferences 未切，或未点 Apply 后重新生成 | 切 External Script Editor → `Assets > Open C# Project` |
| Rider 提示找不到 Unity Editor | Hub 的 secondaryInstallPath 未生效 / Hub 为非标准安装 | `Settings > Unity Engine > Add custom Unity Editor` 手动指定 `D:\Unity\UnityEditor\2022_3_62f3c1` |
| 断点空心、不命中 | ① Editor Attaching 未勾；② 命中的是旧 dll；③ Unity 处于暂停 | 勾 Editor Attaching → Unity `Ctrl+R` 重编译 → 用 Attach & Play 配置；确认 modify 后已编译成功（Console 无红） |
| IntelliSense 报 UnityEngine 全部找不到，但 Unity 编译正常 | 用 sln 而非目录打开了项目 | 关闭当前项目 → File > Open 选**根目录** |
| UniTask / VContainer 只有元数据、跳转失败 | csproj 未生成 Git 包 | 勾选 Generate .csproj for **Git packages** → 重新生成 |
| 每此打开 Rider 都开始下载 NuGet | 自动 Restore 未关 | 关闭 `Automatically restore NuGet packages` |
| 与 Unity 编辑冲突：`x.cs` 修改被回写 | Unity 重新生成/导入时覆盖了磁盘文件修改 | 在 Unity 焦点回到 Editor 后再修改；Rider 会自动 reload（“File Cache Conflict” 提示选 Rider 一侧） |
| 某个 asmdef 未出现在 sln | 该程序集下无 `.cs`（当前 `Presentation` 即如此） | 属正常行为，写入首个脚本后自动出现 |
| Play 后调试自动断开 | Domain Reload（本项目 `m_EnterPlayModeOptionsEnabled: 0`） | 用 “Attach to Unity Editor & Play” 配置，勿手动 Play |
| 切换后出现大量 “previously compiled/renamed” 编译错误 | 残留 asmdef 与遗留 csproj 混用 | 关闭 Unity → 删除 §9.2 列出的失效文件 → 重启 Unity 重生成 |

---

## 12. 回滚方案

迁移未触及任何 `.cs` 源码或资产配置，回滚成本极低：

1. Unity `Preferences > External Tools` 改回原外部编辑器 → `Assets > Open C# Project` 重新生成；
2. 删除项目根自动生成的 `.idea/` 目录（如需彻底干净）；
3. 若同时已执行 §9.1/§9.2 删除：`git` 侧不受影响（均为 ignore 文件），如需复核，以 §1 的基线快照为准；
4. UPM 包变动（§9.4）回滚：在 `Packages/manifest.json` 恢复 `"com.unity.ide.visualstudio": "2.0.22"` 一行即可。

唯一不可逆动作是 §1.4 的提交/stash 之外的删除操作——因此**务必先执行 §1.4 建立锚点**。

---

## 13. 建议执行序列

```
① §1.4  提交/stash 当前未提交重构   ─── 必须最先做
② §2    安装 Rider 2025.1+
③ §8    安装必要的 Marketplace 插件
④ §3.1  Unity Preferences：External Script Editor / Editor Attaching / csproj 范围
⑤ §3.2  Assets > Open C# Project（重新生成解决方案）
⑥ §5.1  File > Open 项目根目录
⑦ §4.2/§5.2  Rider 内 MSBuild / 代码风格 / 版本控制设置
⑧ §6.2  建立 Unity Editor & Play 调试配置
⑨ §10   跑完整验收清单
⑩ §9    清理残留（可在验收通过后做）
```
