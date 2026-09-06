---
name: 04-implementation-order-and-decoupling-design-doc
overview: 撰写《04-项目实现顺序与系统解耦设计.md》至 d:/Unity/Projects/DreamKeep/Docs/，落定四层程序集架构（Core/Domain/Data/Presentation）、三段式通信机制与 P0~P6 实现顺序，P0 以待办清单形式写入文档由用户自行执行，代码零改动。
todos:
  - id: write-doc-section-0-1
    content: 撰写 §0~§1：文档定位与 15 项决策记录（含对旧蓝图共识的取代声明）、第三方插件隔离等级表、10 个既有 .cs 逐个处置表、空目录与失效文件盘点，写入 Docs/04-项目实现顺序与系统解耦设计.md
    status: completed
  - id: write-doc-section-2-3
    content: 撰写 §2~§3：四程序集职责边界与禁入清单、依赖链 Mermaid、三段式通信定义、IDomainEvent 事件类型清单、StS 动作队列差异说明、遗物钩子拦截点实现方式、单回合交互时序图与配置烘焙数据流向图
    status: completed
  - id: write-doc-section-4-5
    content: 撰写 §4~§5：P0 九项可照做待办清单（asmdef/目录改名、noEngineReferences、白名单、VContainer 下移、代码迁移、缺陷修复、失效文件删除）与 P1~P6 批次依赖表、扩展点路径与维护注意事项，完成全文交叉引用校对并定稿
    status: completed
---

## 产品概述
为《梦境守望》（Unity 2022.3 卡牌肉鸽毕设）撰写一份统一参考文档：`Docs/04-项目实现顺序与系统解耦设计.md`。文档以 15 轮咨询共识为唯一架构真源，明确四层程序集（Core/Domain/Data/Presentation）的职责边界、解耦方式、通信机制与数据流向，并给出 P0~P6 实现顺序（P0 为用户可照做的改造待办清单）。

## 核心功能
- **文档定位与决策记录**：登记 15 项已锁决策，声明对项目旧蓝图共识（五件套含 Simulation、泛型事件总线承载遗物钩子、VContainer 在 Application、M5 无头模拟）的取代关系，消除两份真相
- **现有插件与代码盘点**：第三方插件隔离等级表、10 个既有 .cs 逐个处置表、编辑器工具、空目录、失效文件
- **系统解耦设计**：四程序集依赖链（无环）、各层职责与禁入清单、三段式通信（命令进/同步结算/事件缓冲出）、IDomainEvent 事件清单、StS 动作队列与本作的关键差异
- **交互流程与数据流向**：单回合时序（Mermaid）、配置资产到运行时数据的烘焙流、命令上行与事件下行两条通道
- **实现顺序 P0~P6**：P0 九项可照做待办（含 asmdef 改名、noEngineReferences、VContainer 下移、代码迁移），P1~P6 目标与依赖
- **扩展点与维护注意事项**：加词条/遗物/事件的扩展路径、Domain 软隔离残余风险、既有代码缺陷清单、裁剪顺序


## 技术方案
### 交付形态
单个 Markdown 文档，落点 `d:/Unity/Projects/DreamKeep/Docs/04-项目实现顺序与系统解耦设计.md`（与设计稿编号衔接，不进 Assets/ 避免 Unity 导入）。纯文字 + Mermaid 架构图/时序图 + 表格，**不写任何代码实现**（本次范围：只写文档，代码一行不动）。

### 章节结构（对应用户要求的 5 项内容）
| 章节 | 对应需求 |
|---|---|
| §0 文档定位与决策记录（取代旧共识） | 前置 |
| §1 现有插件与代码盘点 | 需求 1 |
| §2 系统解耦设计 | 需求 3 |
| §3 系统间交互流程与数据流向 | 需求 4 |
| §4 实现顺序 P0~P6（P0 待办清单） | 需求 2 |
| §5 扩展点与未来维护注意事项 | 需求 5 |

### 必须承载的既定架构（共识产物，写死不开放）
```mermaid
graph TD
    P[Presentation Unity<br>uGUI/动画 + VContainer组合根 + 日志渲染器] -->|PlayCardCommand 等命令| D[Domain Unity-free<br>运行时数据模型+规则+数值计算+编排]
    D -->|List&lt;IDomainEvent&gt; 事件缓冲| P
    A[Authoring/Data Unity<br>SO配置资产+Odin+烘焙] -->|Bake 纯C#数据| D
    D --> C[Core Unity-free<br>状态机/种子RNG/事件缓冲/IActor]
    C -->|IDomainEvent/种子RNG| D
```
依赖链：`Core ← Domain ← Data ← Presentation`（无环）；Core 硬隔离（overrideReferences 白名单），Domain 软隔离（noEngineReferences:true 挡 UnityEngine，第三方靠约定）；通信三段式，遗物钩子为结算管线拦截点而非事件总线广播。

### 写作纪律
- 设计稿章节号（§4.1 edge1/§4.2 edge6/§4.6/§5/§8）作交叉引用锚点，不复制原文
- 命名遵守用户编码规范（_camelCase 私有字段、E 前缀枚举、SO_/UI_ 前缀、一脚本一类型、#region 仅包对外 API、构造注入 private readonly）
- StS 数值曲线可借鉴比例关系、威胁节奏（纵深压境）需自调——明确写入
- 平衡验证已砍，相关内容标注"已裁剪"并说明理由，不留悬空引用
	
