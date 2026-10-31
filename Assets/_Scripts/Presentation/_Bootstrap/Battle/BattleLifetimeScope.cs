using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.GameRun;
using _DreamKeep.CommonPresentation;
using _DreamKeep.Utility;

namespace _DreamKeep.Presentation
{
    /// <summary>战斗段组合根：作为整局 scope 的子 scope，从父容器取整局状态，按遭遇种类与层数从池里抽一场遭遇与一张背景，
    /// 交给 BattleSetup 一次装配出一场战斗后注册实例，并从配置里抽出单位视图预制体转发给表现入口。
    /// 组合根是唯一允许同时认识配置资产与运行时对象的地方。
    /// 注册判定规则：注册集合 = 所有"根"的依赖闭包。根 = RegisterEntryPoint 的入口类 + RegisterComponentInHierarchy 的场景组件
    /// （将来还有容器创建的对象）；每条注册都必须能回答"谁要它"，答不上来即为多余。
    /// 检查方法：从每个根出发，把它的构造函数参数与 [Inject] 成员递归展开一遍，没被覆盖的就是漏注册。
    /// [Inject] 只给"容器造不了它"的对象（场景 / 层级组件、容器外的预制体）；纯 C# 类一律构造函数注入、不加标记。</summary>
    public class BattleLifetimeScope : LifetimeScope
    {
        [SerializeField] private SO_MonsterPool _monsterPool;
        [SerializeField] private SO_BackgroundPool _backgroundPool;

        [Header("临时：地图落地后删除")]
        [Tooltip("0 表示跟随整局层数，大于 0 时覆盖（用于本地验证强怪池）")]
        [SerializeField] private int _floor;
        [SerializeField] private EEncounterKind _encounterKind;

        protected override void Configure(IContainerBuilder builder_)
        {
            RunState run = Parent.Container.Resolve<RunState>();
            // 段会话句柄住整局容器：本段把自己的门面写进去，常驻 UI 据此响应或静默
            BattleSession session = Parent.Container.Resolve<BattleSession>();
            int floor = _floor > 0 ? _floor : run.Floor;

            // 先抽遭遇再洗牌：遭遇抽取会推进整局随机流，顺序固定才能保证同种子可复现
            IReadOnlyList<SO_Encounter> encounters = _monsterPool.Select(_encounterKind, floor);
            SO_Encounter encounter = run.Rng.Pick(encounters);
            if (encounter == null)
                throw new InvalidOperationException(
                    $"第 {floor} 层的 {_encounterKind} 遭遇池为空（{nameof(SO_MonsterPool)} 未配置该档）");

            BattleContext battleContext = BattleSetup.Create(run.Player, encounter, run.Config.StartEnergy, run.Rng);

            BattleEventDispatcher dispatcher = new BattleEventDispatcher();
            // 遗物播报器住整局容器（Run 级单例）：战斗效果与休息处挖掘共用同一涟漪通道，本段只取用
            RelicTriggerNotifier relicNotifier = Parent.Container.Resolve<RelicTriggerNotifier>();
            BattleLoop loop = new BattleLoop(battleContext, run.Config.DrawPerTurn, dispatcher);

            // 遗物系统（GameRun 程序集）：遍历玩家整局持有的遗物，逐件烘焙效果并挂上本场事件流；
            // 战斗域零感知（装配反转），效果实例随本容器销毁。播报器是本段表现出口，遗物生效/用尽经它通知遗物栏
            foreach (var relic in battleContext.Player.Relics)
                RelicBaker.Bake(relic, battleContext, loop.ActionSystem, dispatcher, relicNotifier);

            // 背景抽取放在洗牌之后：它同样消耗整局随机流，放在洗牌前会让牌序跟着背景变化而偏移
            SO_Background background = run.Rng.Pick(_backgroundPool.Select(_encounterKind));

            // 表现流：弃牌波抖动这类装饰性随机单独一条，绝不推进整局的玩法随机（洗牌与随机目标）
            builder_.RegisterInstance(new Rng(Environment.TickCount)).Keyed("Presentation");

            builder_.RegisterInstance(battleContext); // 预留：供 HUD 等视图 [Inject] 读取血量 / 能量
            builder_.RegisterInstance(dispatcher); // 供视图 [Inject] 注册事件处理器（Register<TEvent>）
            builder_.RegisterInstance(relicNotifier); // 供遗物栏 [Inject] 订阅遗物播报
            builder_.RegisterInstance(new BattleCommandDispatcher(loop)); // 被视图的 [Inject] 要（出牌命令）
            builder_.RegisterInstance(new BattleQueryDispatcher(battleContext)); // 被视图的 [Inject] 要（预览查询与牌堆张数）
            builder_.RegisterInstance(loop); // 被 BattleEntry 的构造函数要
            // 单位视图预制体一律来自配置资产：玩家取自整局角色配置，敌人列表与遭遇同源同序，
            // 由本组合根抽出转发，BattleEntry 无需认识配置
            builder_.RegisterInstance(run.Config.ViewPrefab).Keyed("Player");
            builder_.RegisterInstance(EnemyViewPrefabs(encounter));

            builder_.RegisterComponentInHierarchy<UI_HandView>();
            // 瞄准器与本体同挂一个节点：它要上行门面现算整手牌的数值预览，无每帧入口、由目标提示驱动在下发结果时驱动
            builder_.RegisterComponentInHierarchy<UI_HandAim>();
            // 演出器与本体同挂一个节点：它订阅全部十一条事件，注册位置紧随本体，保住"手牌演出先于玩家 cast"的时序
            builder_.RegisterComponentInHierarchy<UI_HandShow>();
            // 三堆不订阅任何事件：数字由手牌视图在飞行的起飞与落地那一刻通知（谁飞谁通知），注册顺序不再承载时序契约
            builder_.RegisterComponentInHierarchy<UI_DrawPile>();
            builder_.RegisterComponentInHierarchy<UI_DiscardPile>();
            builder_.RegisterComponentInHierarchy<UI_ExhaustPile>();
            // 牌堆查看面板：预摆默认失活，三堆点击经 UI_Pile 转发打开；自己订阅卡牌事件做差分刷新
            builder_.RegisterComponentInHierarchy<UI_PileCardsView>();
            builder_.RegisterComponentInHierarchy<UI_EnergyRemain>();
            builder_.RegisterComponentInHierarchy<UI_EndTurnButton>();
            // 弹层与特效编排挂在段根上；弹层工厂与特效工厂注册在父容器（池要跨段复用），构造注入即可取到
            builder_.RegisterComponentInHierarchy<UI_BattlePopRoot>(); // 战斗弹层编排：订伤害/回血/回合开始，决定弹什么、弹在哪
            // 战斗特效编排：订伤害/回血，决定播什么、摆在哪；与弹层编排各自独立订阅同一事件
            builder_.RegisterComponentInHierarchy<UI_BattleVfxRoot>();
            builder_.RegisterComponentInHierarchy<BattleTargetDirector>(); // 瞄准中间件：持唯一的目标箭头与当前瞄准客户端，按玩法层预览点亮目标单位
            builder_.RegisterComponentInHierarchy<UI_TargetArrow>(); // 目标箭头：战斗通用视图，住 Canvas_HUD/TargetingLayer，由瞄准中间件独占驱动
            builder_.RegisterComponentInHierarchy<UI_ActorRoot>(); // 战场单位区：槽位点坐标来源（出生摆位与移动补间）+ 单位视图登记表（目标提示按生物查视图）
            builder_.RegisterComponentInHierarchy<UI_BackgroundView>(); // 背景层视图：本场背景在构建完成后一次写入
            // 场景里预摆的悬停发起器既不经过池、也不会被 RegisterComponentInHierarchy 逐个覆盖：装配期只对组件本身补一次注入。
            // 常驻发起器已由整局层构建回调注入（那时段还没生成、扫不到段内实例），此处覆盖段内预摆与池化两种；
            // 重复注入同一实例只是把 _host 再赋值一次，无副作用。刻意不注入整个层级，避免把已注册场景组件
            // （如 EndTurnButton / EnergyRemain）的 [Inject] 再跑一遍造成重复订阅。
            // 会话必须在容器建好之后才能写入：Configure 期间取不到自己刚注册的场景组件
            builder_.RegisterBuildCallback(resolver_ =>
            {
                foreach (UI_HoverTip tip in FindObjectsOfType<UI_HoverTip>(true)) resolver_.Inject(tip);
                resolver_.Resolve<UI_BackgroundView>().Apply(background);
                session.Open(resolver_.Resolve<BattleCommandDispatcher>(), resolver_.Resolve<BattleQueryDispatcher>(),
                    dispatcher, resolver_.Resolve<BattleTargetDirector>(), resolver_.Resolve<UI_ActorRoot>(),
                    relicNotifier, loop, encounter.Reward);
            });
            builder_.RegisterEntryPoint<BattleEntry>();
        }

        // 敌人视图预制体列表与遭遇同源派生：下标即敌人下标，未接线的配置留 null 交生成侧报错跳过该敌人
        private static List<GameObject> EnemyViewPrefabs(SO_Encounter encounter_)
        {
            IReadOnlyList<SO_Enemy> configs = encounter_.Enemies;
            List<GameObject> prefabs = new(configs.Count);
            for (int i = 0; i < configs.Count; i++)
                prefabs.Add(configs[i].ViewPrefab);
            return prefabs;
        }
    }
}
