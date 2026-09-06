# DreamKeep · 梦境守望

> 单人卡牌策略 Roguelike · 计算机本科毕业设计 · 目标平台 PC

沉睡者的意识世界。你是**守梦人**——沉眠中唯一清醒的自我，梦魇正沿梦的走廊向觉醒边界压境。

---

## 环境要求

| 项 | 值 |
| --- | --- |
| Unity | `2022.3.62f3c1`（LTS 国内版 c1） |
| 渲染管线 | Built-in |
| Git LFS | **必需**（美术 / 音频资源走 LFS，未安装会导致签出失败） |
| IDE | Rider 或 Visual Studio 2022 |

## 技术栈

| 类别 | 选型 |
| --- | --- |
| 依赖注入 | VContainer `1.19.0` |
| 异步 | UniTask |
| 编辑器扩展 | Odin Inspector + Odin Validator |
| 补间动画 | DOTween |
| 序列化 | Newtonsoft.Json `3.2.2` |
| 2D / UI | 2D Feature、uGUI、TextMeshPro |
| 其他 | Timeline、PostProcessing、Visual Scripting、Test Framework |

## 架构

一句话总纲：**四个程序集、一条无环依赖链、三段式通信；Core 硬隔离，Domain 软隔离；规则与状态只存在一处，表现层只读不回写。**

依赖链：`Core ← Domain ← Data ← Presentation`

> 分层职责、通信机制与实现顺序以 [Docs/04](Docs/04-项目实现顺序与系统解耦设计.md) 为准。

## 文档

| 文档 | 内容 |
| --- | --- |
| [Docs/03-毕业设计方案-卡牌肉鸽融合成熟玩法.md](Docs/03-毕业设计方案-卡牌肉鸽融合成熟玩法.md) | 纯游戏设计稿（v3.5）：玩法 / 数值 / 题材定案，不含实现 |
| [Docs/04-项目实现顺序与系统解耦设计.md](Docs/04-项目实现顺序与系统解耦设计.md) | 架构定案与 P0 待办清单：「代码放哪、依赖谁、先做什么」以此为准 |

## 快速开始

```bash
git clone https://github.com/mzyqzmd/DreamKeep.git
cd DreamKeep
git lfs install   # 首次必须，否则签出的是 LFS 指针文件而非真实资源
git lfs pull
```

1. Unity Hub → `Add project from disk` → 选择本仓库根目录
2. 等待 `Library/` 首次导入完成
3. 打开 `Assets/Scenes/SampleScene.unity` 运行

## 仓库约定

- `Assets/_Art/`、`Assets/_Data/`、`Assets/_Prefabs/`、`Assets/_Scripts/` 为项目自有内容；`Assets/Plugins/` 与 `Assets/Editor/` 为第三方插件与工具集
- 提交前用 `git lfs ls-files` 确认本次新增的二进制资源已被 LFS 接管
- `.codebuddy/`、`.lingma/` 为本地 AI 辅助工具目录，已忽略，不进仓库
