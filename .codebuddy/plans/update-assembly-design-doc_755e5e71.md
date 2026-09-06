---
name: update-assembly-design-doc
overview: 将《制作流程与技术选型.md》的程序集相关章节更新为实际落地的五件套设计（_DreamKeep. 前缀、放弃自建 Editor 程序集、Simulation 引用修正），并同步项目记忆中的过时信息。
todos:
  - id: update-asmdef-table
    content: 修订 §2.2 程序集表：名称加 _ 前缀、删除 DreamKeep.Editor 行、Simulation 引用改 Core+Data、补 Assembly-CSharp-Editor 说明
    status: completed
  - id: update-dir-structure
    content: 修订 §3 目录结构：Editor 目录现状说明、asmdef 范围限定为 _Scripts 五目录
    status: completed
    dependencies:
      - update-asmdef-table
  - id: update-m0-stage
    content: 修订 M0 阶段：六件套改五件套，标注已完成项（asmdef、插件包），同步验收标准
    status: completed
    dependencies:
      - update-asmdef-table
  - id: add-progress-section
    content: 新增“当前进度与下一步开发”章节：现状盘点 + M0 剩余工作清单（事件总线/VContainer/GameRoot/SO 骨架）
    status: completed
    dependencies:
      - update-m0-stage
  - id: fix-stale-memory
    content: 更新项目记忆 72524715：种子脚本已移除、空字符串遗留项已清理
    status: completed
    dependencies:
      - add-progress-section
---

## 产品概述
对《梦境守望》施工蓝图文档（Assets/Documents/制作流程与技术选型.md，当前 v1.0）进行一次“程序集设计对齐”修订，使文档与工程实际状态一致，并新增“当前进度与下一步开发”章节，明确接下来的开发任务。

## 核心功能
- **§2.2 程序集表修订**：程序集名改为实际的下划线前缀（_DreamKeep.Core 等五件套）；删除已放弃的 DreamKeep.Editor 行，改为说明“编辑器工具放 Assets/Editor，走 Unity 自动 Assembly-CSharp-Editor”；Simulation 行引用列由“Core、Data、Newtonsoft”修正为“Core、Data”（Newtonsoft 依赖预编译 DLL Auto Reference 自动生效，无需显式引用）
- **§3 目录结构修订**：补充 Assets/Editor 现状（ToolBoxHub，16 个 .cs），“每目录一个 asmdef”限定为 _Scripts 下五目录
- **M0 阶段修订**：“asmdef 六件套”改为“五件套”，验收标准同步核对
- **新增“当前进度与下一步”章节**：记录实际现状（五个程序集均为空壳无脚本、场景仅 SampleScene、插件包已就位：VContainer 1.19.0/UniTask/Newtonsoft/postprocessing/test-framework），并列出 M0 剩余工作清单（事件总线+EditMode 测试、VContainer 接入与 GameRoot 场景、SO 数据骨架种子脚本、五场景占位）
- **修正过时记忆**：种子脚本已不存在、Presentation asmdef 空字符串遗留项已清理
