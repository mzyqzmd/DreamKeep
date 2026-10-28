using UnityEngine;
using VContainer;
using VContainer.Unity;
using _DreamKeep.ConfigData;
using _DreamKeep.CommonPresentation;
using _DreamKeep.GameRun;

namespace _DreamKeep.Presentation
{
    /// <summary>整局组合根：一局游戏期间常驻，持有跨战斗延续的整局状态，以及只与整局有关的服务与 UI。
    /// 战斗段作为它的子 scope 从父容器取 RunState，战斗结束只销毁子 scope，整局状态与这里注册的常驻视图不受影响。</summary>
    public class RunLifetimeScope : LifetimeScope
    {
        [SerializeField] private SO_Player _playerConfig;
        [SerializeField] private int _seed = 12345;

        protected override void Configure(IContainerBuilder builder_)
        {
            builder_.RegisterInstance(new RunState(_playerConfig, _seed));
            // 段会话句柄：段在容器构建完成时写入自己的门面、销毁前清空，常驻 UI 只认它（见 BattleSession）
            builder_.RegisterInstance(new BattleSession());
            // 弹层与特效工厂注册在整局层：池要跨段复用，段内编排者（BattlePopRoot / BattleVfxRoot）从父容器解析即可，
            // 段根 prefab 因此不必引用场景里的常驻对象
            builder_.RegisterComponentInHierarchy<PopFactory>();
            builder_.RegisterComponentInHierarchy<VfxFactory>();
            // 整局 UI：跟随这一整局，进战斗后才有响应，段不在场时静默
            builder_.RegisterComponentInHierarchy<UI_RelicsGroup>();
            builder_.RegisterComponentInHierarchy<UI_PotionGroup>();
            builder_.RegisterComponentInHierarchy<UI_TopBar>(); // 装配完即自激活显示
            // 段场景加载管理器：地图落地前由它 additive 加载战斗段（及未来的商店/事件/休息段）并在打完后卸载，它也要拿到会话句柄
            builder_.RegisterComponentInHierarchy<SegmentSceneManager>();
            // 主菜单 / 奖励面板不在此注册：RegisterComponentInHierarchy 要求组件必须存在于场景，缺节点会让容器构建失败；
            // 二者由加载器在 Start 里查找并 InjectGameObject 补注入
            // 常驻发起器随整局容器装配：PopFactory 本就注册在这层，段不在场时悬停也能弹气泡；
            // 段内预摆/池化的实例由 BattleLifetimeScope 的构建回调兜底（整局层回调更早跑、那时段还没生成，漏不掉它们）
            builder_.RegisterBuildCallback(resolver_ =>
            {
                foreach (UI_HoverTip tip in FindObjectsOfType<UI_HoverTip>(true)) resolver_.Inject(tip);
            });
        }
    }
}
