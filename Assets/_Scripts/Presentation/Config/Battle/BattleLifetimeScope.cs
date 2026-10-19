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
    /// 并从配置里抽出单位视图预制体转发给表现入口——换一份配置就是换一个玩家 / 一批敌人；
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
            // 随机流分两条、各带 key：玩法的洗牌与随机目标走域流（经 BattleContext.Rng 取），表现层的装饰性随机（弃牌波抖动）走表现流。
            // 两条都带 key 之后，任何不加 key 的 [Inject] Rng 都会在容器构建期直接报错——谁想要哪条流必须在注入点写清楚
            builder_.RegisterInstance(rng).Keyed("Battle"); // 域流：当前无 [Inject] 消费者，刻意留口
            builder_.RegisterInstance(new Rng(System.Environment.TickCount)).Keyed("Presentation"); // 表现流：弃牌波起落点抖动
            BattleContext battleContext = BattleSetup.Create(_playerConfig, _enemyConfigs, rng);

            BattleEventDispatcher dispatcher = new BattleEventDispatcher();
            BattleLoop loop = new BattleLoop(battleContext, _playerConfig.DrawPerTurn, dispatcher);

            builder_.RegisterInstance(battleContext); // 预留：供 HUD 等视图 [Inject] 读取血量 / 能量
            builder_.RegisterInstance(dispatcher); // 供视图 [Inject] 注册事件处理器（Register<TEvent>）
            builder_.RegisterInstance(new BattleCommandDispatcher(loop)); // 被视图的 [Inject] 要（出牌命令）
            builder_.RegisterInstance(new BattleQueryDispatcher(battleContext)); // 被视图的 [Inject] 要（预览查询与牌堆张数）
            builder_.RegisterInstance(loop); // 被 BattleEntry 的构造函数要
            // 单位视图预制体一律来自配置资产：玩家走工厂注册（服务类型 GameObject，未接线留 null 交生成侧报错跳过），
            // 敌人列表与 _enemyConfigs 同源同序，由本组合根抽出转发，BattleEntry 无需认识配置
            builder_.RegisterInstance(_playerConfig.ViewPrefab).Keyed("Player");
            builder_.RegisterInstance(EnemyViewPrefabs());

            builder_.RegisterComponentInHierarchy<UI_HandView>();
            // 演出器与本体同挂一个节点：它订阅全部十一条事件，注册位置紧随本体，保住"手牌演出先于玩家 cast"的时序
            builder_.RegisterComponentInHierarchy<UI_HandShow>();
            // 三堆不订阅任何事件：数字由手牌视图在飞行的起飞与落地那一刻通知（谁飞谁通知），注册顺序不再承载时序契约
            builder_.RegisterComponentInHierarchy<UI_DrawPile>();
            builder_.RegisterComponentInHierarchy<UI_DiscardPile>();
            builder_.RegisterComponentInHierarchy<UI_ExhaustPile>();
            builder_.RegisterComponentInHierarchy<UI_EnergyRemain>();
            builder_.RegisterComponentInHierarchy<UI_EndTurnButton>();
            builder_.RegisterComponentInHierarchy<UI_PopTextRoot>(); // 弹层宿主：[Inject] 订阅 BE_TurnStarted 转发回合横幅
            builder_.RegisterComponentInHierarchy<UI_NumberPopDirector>(); // 数字漂字导演：订伤害/回血事件，按语义驱动弹层
            builder_.RegisterComponentInHierarchy<UI_TargetDirector>(); // 目标提示驱动：读手牌瞄准事实，按玩法层预览点亮目标单位
            builder_.RegisterComponentInHierarchy<UI_ActorRoot>(); // 战场单位区：槽位点坐标来源（出生摆位与移动补间）+ 单位视图登记表（目标提示按生物查视图）
            // 场景里预摆的悬停发起器既不经过池、也不会被 RegisterComponentInHierarchy 逐个覆盖：装配期只对组件本身补一次注入。
            // 刻意不注入整个层级，避免把已注册场景组件（如 EndTurnButton / EnergyRemain）的 [Inject] 再跑一遍造成重复订阅。
            builder_.RegisterBuildCallback(resolver_ =>
            {
                foreach (UI_HoverTip tip in FindObjectsOfType<UI_HoverTip>(true)) resolver_.Inject(tip);
            });
            builder_.RegisterEntryPoint<BattleEntry>();
        }

        // 敌人视图预制体列表与配置同源派生：下标即敌人下标，未接线的配置留 null 交生成侧报错跳过该敌人
        private List<GameObject> EnemyViewPrefabs()
        {
            List<GameObject> prefabs = new(_enemyConfigs.Count);
            foreach (SO_Enemy enemyConfig in _enemyConfigs)
                prefabs.Add(enemyConfig.ViewPrefab);
            return prefabs;
        }
    }
}