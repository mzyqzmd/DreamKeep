using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using _DreamKeep.Data;
using _DreamKeep.Gameplay;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Presentation
{
    /// <summary>组合根：在 Inspector 上挂玩家与敌人配置，交给 BattleSetup 一次装配出一场战斗后注册实例。
    /// 组合根是唯一允许同时认识配置资产与运行时对象的地方。</summary>
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private SO_Player _playerConfig;
        [SerializeField] private List<SO_Enemy> _enemyConfigs = new();
        [SerializeField] private int _seed = 12345;

        protected override void Configure(IContainerBuilder builder_)
        {
            BattleContext battleContext = BattleSetup.Create(_playerConfig, _enemyConfigs, _seed);

            BattleEventDispatcher dispatcher = new BattleEventDispatcher();
            BattleLoop loop = new BattleLoop(battleContext, _playerConfig.DrawPerTurn, dispatcher);

            builder_.RegisterInstance(battleContext);
            builder_.RegisterInstance(dispatcher);
            builder_.RegisterInstance(new BattleCommandGateway(loop));
            builder_.RegisterInstance(loop);

            builder_.RegisterComponentInHierarchy<Obj_HandView>();
            builder_.RegisterEntryPoint<BattleBootstrap>();
        }
    }
}
