---
name: AffixExecutionM0
overview: 基于已达成的设计共识，在 Core/Data/Application 层补齐词条运行时数据、结算器与执行上下文，并改造 SO_Affix 为最小拍平结构，使 M0 灰卡端到端链路能实际执行 Damage/Block/Draw 效果。
todos:
  - id: core-affix-runtime
    content: 创建 Core 层运行时词条数据与结算器
    status: completed
  - id: data-affix-flatten
    content: 改造 SO_Affix 拍平字段并暴露属性
    status: completed
    dependencies:
      - core-affix-runtime
  - id: app-combat-cardplayer
    content: 实现 Application 层 CombatModel 与 CardPlayer
    status: completed
    dependencies:
      - core-affix-runtime
  - id: editmode-tests
    content: 编写 EditMode 测试覆盖 Damage/Block/Draw 结算
    status: completed
    dependencies:
      - app-combat-cardplayer
  - id: run-tests-fix-refs
    content: 运行测试并修复跨程序集引用与命名规范问题
    status: completed
    dependencies:
      - editmode-tests
---

## 产品概述
在已有的 `SO_Affix`（词条模板）、`SO_Card`（卡牌模板）与 `ECardType`（卡牌类型）基础上，补齐词条的运行时数据结构、执行上下文、结算器与战斗模型接口，使卡牌打出时能够按词条参数正确结算伤害、格挡与抽牌，形成 M0 阶段“灰卡端到端”可跑通的最小闭环。

## 核心功能
- 改造 `SO_Affix` 为最小效果单元：名称、可选描述、卡面显示描述开关、效果类型、数值、次数、状态 ID
- Core 层新增 `EAffixEffect`、`AffixData`、`EffectContext`、`ICombatModel`、`AffixResolver`
- Application 层新增 `CombatModel`、`Actor`、`CardPlayer`，完成 `SO_Affix` → `AffixData` 的映射与目标上下文注入
- 编写 EditMode 测试覆盖 Damage / Block / Draw 三条结算路径

[object Object]]]>
