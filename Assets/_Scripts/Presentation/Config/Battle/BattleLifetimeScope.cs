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
        [SerializeField] private int _seed = 12345;

        protected override void Configure(IContainerBuilder builder_)
        {
            Rng rng = new(_seed);
            BattleContext battleContext = BattleSetup.Create(_playerConfig, _enemyConfigs, rng);

            BattleEventDispatcher dispatcher = new BattleEventDispatcher();
            BattleLoop loop = new BattleLoop(battleContext, _playerConfig.DrawPerTurn, dispatcher);

            builder_.RegisterInstance(battleContext); // 预留：供 HUD 等视图 [Inject] 读取血量 / 能量
            builder_.RegisterInstance(dispatcher); // 供视图 [Inject] 注册事件处理器（Register<TEvent>）
            builder_.RegisterInstance(new BattleCommandDispatcher(loop)); // 被 UI_HandView 的 [Inject] 要（转交给出牌流程发命令）
            builder_.RegisterInstance(loop); // 被 BattleEntry 的构造函数要

            builder_.RegisterComponentInHierarchy<UI_HandView>();
            // 三堆不订阅任何事件：数字由打牌流程在飞行的起飞与落地那一刻通知（谁飞谁通知），注册顺序不再承载时序契约
            builder_.RegisterComponentInHierarchy<UI_DrawPile>();
            builder_.RegisterComponentInHierarchy<UI_DiscardPile>();
            builder_.RegisterComponentInHierarchy<UI_ExhaustPile>();
            builder_.RegisterComponentInHierarchy<UI_EnergyRemain>();
            builder_.RegisterComponentInHierarchy<UI_EndTurnButton>();
            builder_.RegisterEntryPoint<BattleEntry>();
        }
    }
}