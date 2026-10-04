using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.Utility;

namespace _DreamKeep.Presentation
{
    /// <summary>战斗段组合根：在 Inspector 上挂玩家与敌人配置，交给 BattleSetup 一次装配出一场战斗后注册实例；
    /// 组合根是唯一允许同时认识配置资产与运行时对象的地方。
    /// 注册判定规则：注册集合 = 所有"根"的依赖闭包。根 = RegisterEntryPoint 的入口类 + RegisterComponentInHierarchy 的场景组件
    /// （将来还有容器创建的对象）；每条注册都必须能回答"谁要它"，答不上来即为多余。
    /// 检查方法：从每个根出发，把它的构造函数参数与 [Inject] 成员递归展开一遍，没被覆盖的就是漏注册。
    /// [Inject] 只给"容器造不了它"的对象（场景 / 层级组件、容器外的预制体）；纯 C# 类一律构造函数注入、不加标记。</summary>
    public class BattleLifetimeScope : LifetimeScope
    {
        [SerializeField] private SO_Player _playerConfig;
        [SerializeField] private List<SO_Enemy> _enemyConfigs = new();

        /// <summary>敌人视图预制体：与 _enemyConfigs 平行索引，元素根上挂具体敌人类（如 UI_EggKnight）与 UI_ActorAnimator，由 BattleEntry 运行时生成。</summary>
        [SerializeField] private List<GameObject> _enemyViewPrefabs = new();
        [SerializeField] private int _seed = 12345;

        protected override void Configure(IContainerBuilder builder_)
        {
            Rng rng = new(_seed);
            builder_.RegisterInstance(rng);
            BattleContext battleContext = BattleSetup.Create(_playerConfig, _enemyConfigs, rng);

            BattleEventDispatcher dispatcher = new BattleEventDispatcher();
            BattleLoop loop = new BattleLoop(battleContext, _playerConfig.DrawPerTurn, dispatcher);

            builder_.RegisterInstance(battleContext); // 预留：供 HUD 等视图 [Inject] 读取血量 / 能量
            builder_.RegisterInstance(dispatcher); // 供视图 [Inject] 注册事件处理器（Register<TEvent>）
            builder_.RegisterInstance(new BattleCommandDispatcher(loop)); // 被 UI_HandView 的 [Inject] 要（转交给出牌流程发命令）
            builder_.RegisterInstance(new ActorViewRegistry()); // 单位视图注册表：敌人视图运行时生成，目标提示按目标生物查视图
            builder_.RegisterInstance(loop); // 被 BattleEntry 的构造函数要
            builder_.RegisterInstance(_enemyViewPrefabs); // 被 BattleEntry 的构造函数要（生成敌人视图）

            builder_.RegisterComponentInHierarchy<UI_HandView>();
            // 三堆不订阅任何事件：数字由打牌流程在飞行的起飞与落地那一刻通知（谁飞谁通知），注册顺序不再承载时序契约
            builder_.RegisterComponentInHierarchy<UI_DrawPile>();
            builder_.RegisterComponentInHierarchy<UI_DiscardPile>();
            builder_.RegisterComponentInHierarchy<UI_ExhaustPile>();
            builder_.RegisterComponentInHierarchy<UI_EnergyRemain>();
            builder_.RegisterComponentInHierarchy<UI_EndTurnButton>();
            builder_.RegisterComponentInHierarchy<UI_PopTextRoot>(); // 弹层宿主：[Inject] 订阅 BE_TurnStarted 转发回合横幅
            builder_.RegisterComponentInHierarchy<UI_Player>(); // 场景注册的单位即玩家：注入时自绑玩家生物并登记注册表
            builder_.RegisterComponentInHierarchy<UI_TargetTipDirector>(); // 目标提示驱动：读手牌瞄准事实，按玩法层预览点亮目标单位
            builder_.RegisterComponentInHierarchy<UI_ActorSlotGroup>(); // 战场槽位区：玩家与敌人视图的槽位点坐标来源（出生摆位与移动补间）
            builder_.RegisterEntryPoint<BattleEntry>();
        }
    }
}