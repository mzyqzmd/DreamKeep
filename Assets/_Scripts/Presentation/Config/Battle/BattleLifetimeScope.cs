using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.Utility;

namespace _DreamKeep.Presentation
{
    /// <summary>战斗段组合根：在 Inspector 上挂玩家与敌人配置，交给 BattleSetup 一次装配出一场战斗后注册实例，
    /// 并从配置里抽出玩家与敌人的视图预制体转发给表现入口——换一份配置就是换一个玩家 / 一批敌人；
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
            builder_.RegisterInstance(rng);
            BattleContext battleContext = BattleSetup.Create(_playerConfig, _enemyConfigs, rng);

            BattleEventDispatcher dispatcher = new BattleEventDispatcher();
            BattleLoop loop = new BattleLoop(battleContext, _playerConfig.DrawPerTurn, dispatcher);

            builder_.RegisterInstance(battleContext); // 预留：供 HUD 等视图 [Inject] 读取血量 / 能量
            builder_.RegisterInstance(dispatcher); // 供视图 [Inject] 注册事件处理器（Register<TEvent>）
            builder_.RegisterInstance(new BattleCommandDispatcher(loop)); // 被 UI_HandView 的 [Inject] 要（转交给出牌流程发命令）
            builder_.RegisterInstance(new ActorViewRegistry()); // 单位视图注册表：玩家与敌人视图运行时生成，目标提示按目标生物查视图
            builder_.RegisterInstance(loop); // 被 BattleEntry 的构造函数要
            // 视图预制体一律来自配置资产，由本组合根抽出后转发，BattleEntry 无需认识配置
            builder_.Register<GameObject>(_ => _playerConfig.ViewPrefab, Lifetime.Singleton); // 被 BattleEntry 的构造函数要（生成玩家视图）
            builder_.RegisterInstance(EnemyViewPrefabs()); // 被 BattleEntry 的构造函数要（生成敌人视图，与敌人配置平行索引）

            builder_.RegisterComponentInHierarchy<UI_HandView>();
            // 三堆不订阅任何事件：数字由打牌流程在飞行的起飞与落地那一刻通知（谁飞谁通知），注册顺序不再承载时序契约
            builder_.RegisterComponentInHierarchy<UI_DrawPile>();
            builder_.RegisterComponentInHierarchy<UI_DiscardPile>();
            builder_.RegisterComponentInHierarchy<UI_ExhaustPile>();
            builder_.RegisterComponentInHierarchy<UI_EnergyRemain>();
            builder_.RegisterComponentInHierarchy<UI_EndTurnButton>();
            builder_.RegisterComponentInHierarchy<UI_PopTextRoot>(); // 弹层宿主：[Inject] 订阅 BE_TurnStarted 转发回合横幅
            builder_.RegisterComponentInHierarchy<UI_TargetTipDirector>(); // 目标提示驱动：读手牌瞄准事实，按玩法层预览点亮目标单位
            builder_.RegisterComponentInHierarchy<UI_ActorRoot>(); // 战场槽位区：玩家与敌人视图的槽位点坐标来源（出生摆位与移动补间）
            builder_.RegisterEntryPoint<BattleEntry>();
        }

        // 敌人视图预制体与敌人配置同源派生：下标即敌人下标，未接线的配置留 null 交生成侧报错跳过
        private List<GameObject> EnemyViewPrefabs()
        {
            List<GameObject> viewPrefabs = new(_enemyConfigs.Count);
            foreach (SO_Enemy enemyConfig in _enemyConfigs)
                viewPrefabs.Add(enemyConfig.ViewPrefab);
            return viewPrefabs;
        }
    }
}