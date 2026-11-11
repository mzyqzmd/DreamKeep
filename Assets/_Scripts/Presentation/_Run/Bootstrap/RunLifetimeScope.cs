using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using _DreamKeep.ConfigData;
using _DreamKeep.CommonPresentation;
using _DreamKeep.EngineUtility;
using _DreamKeep.GameRun;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Presentation
{
    /// <summary>整局组合根（住 Scene_Run，随局生灭）：一局游戏期间 additive 常驻，持有跨战斗延续的整局状态，
    /// 以及只与整局有关的服务与 UI；战斗段作为它的子 scope 从父容器取 RunState，战斗结束只销毁子 scope。
    /// autoRun 关闭：Scene_Run 由常驻主菜单编排者黑屏加载后手动 Build，容器建完才允许进先古段；
    /// 每次进局即新容器新种子，重开一把=本 scope 随场景卸载重建，无跨局残留。</summary>
    public class RunLifetimeScope : LifetimeScope
    {
        [Tooltip("整局随机种子：编辑器下用固定值复现调试（Inspector 可改）；构建版忽略此值，恒取运行时熵源")] [SerializeField]
        private int _seed = 12345;

        [SerializeField] private SO_Player _playerConfig;
        [SerializeField] private SO_MapGenerationConfig _mapConfig;

        [Tooltip("兜底遗物池：常规池抽空时由 RelicRoller 降级发放，池内只放头环")] [SerializeField]
        private SO_RelicPool _fallbackRelicPool;

        // 常驻服务：住 Scene_MainMenu，本场景取不到，只能由常驻编排者在本容器 Build 之前转交
        private AudioRoot _audioRoot;
        private ScreenShakeDriver _shakeDriver;

        /// <summary>转交常驻服务：由常驻编排者在 Build 之前直调，Configure 里按实例注册给下游注入。</summary>
        public void BindPersistentServices(AudioRoot audioRoot_, ScreenShakeDriver shakeDriver_)
        {
            _audioRoot = audioRoot_;
            _shakeDriver = shakeDriver_;
        }

        protected override void Configure(IContainerBuilder builder_)
        {
            if (!_mapConfig) throw new InvalidOperationException("RunLifetimeScope._mapConfig 未接线，无法生成整局地图");
#if UNITY_EDITOR
            int seed = _seed; // 编辑器：固定种子复现调试，Inspector 可改
#else
            int seed = Environment.TickCount ^ Guid.NewGuid().GetHashCode(); // 构建版：运行时熵源，肉鸽每局不同
#endif
            Debug.Log($"[RunLifetimeScope] 整局随机种子 {seed}");
            // 读档分支：快照静态暂存跨过场景加载，容器 Build 之前先按存档重建整局状态，避免常规构造器重新生成地图与卡组
            ISaveReader pending = SaveService.HasPendingRestore
                ? SaveService.PendingReaderOf(typeof(RunState).FullName)
                : null;
            RunState run = new(_playerConfig, seed, _mapConfig, _fallbackRelicPool);
            if (pending != null)
            {
                try { run = RunStateRestore.Build(pending, _playerConfig, _mapConfig, _fallbackRelicPool); }
                catch (Exception e)
                {
                    Debug.LogError($"[RunLifetimeScope] 整局状态重建失败，删档并退回新局：{e.Message}");
                    SaveService.Delete();
                    SaveService.ClearRestoreIntent();
                }
            }

            builder_.RegisterInstance(run).AsSelf().As<ISaveable>();
            // 整局段推进：阶段位、选点写盘与待领清单清理归域侧（见 RunProgressService）
            RunProgressService progress = new(run);
            builder_.RegisterInstance(progress).AsSelf().As<ISaveable>();
            // 整局命令入口：常驻界面上报计时、战斗外药水与领奖意图，写域在域侧执行（见 RunCommandDispatcher）
            RewardClaimService rewards = new(run);
            builder_.RegisterInstance(rewards);
            builder_.RegisterInstance(new RunCommandDispatcher(run, rewards));
            // 遗物播报器：整局一个，跨段存活——战斗遗物效果与休息处挖掘领遗物共用同一涟漪通道（见 UI_RelicsGroup）
            builder_.RegisterInstance(new RelicTriggerNotifier());
            // 段会话句柄：段在容器构建完成时写入自己的门面、销毁前清空，常驻 UI 只认它（见 BattleSession）
            builder_.RegisterInstance(new BattleSession());
            // 表现侧孪生句柄：战斗段表现锚点（单位根 / 目标裁决）随它开关，常驻药水栏经它拿每帧引用（见 BattleStage）
            builder_.RegisterInstance(new BattleStage());
            // 弹层与特效工厂注册在整局层：池要跨段复用，段内编排者（BattlePopRoot / BattleVfxRoot）从父容器解析即可，
            // 段根 prefab 因此不必引用场景里的常驻对象
            builder_.RegisterComponentInHierarchy<PopFactory>();
            builder_.RegisterComponentInHierarchy<PopPanelFactory>();
            builder_.RegisterComponentInHierarchy<VfxFactory>();
            builder_.RegisterComponentInHierarchy<VfxDefaultRoot>(); // 常驻特效默认根：战斗外玩家回血在屏幕中心播一次
            // 2D 音频根：固定槽位池+优先级抢占，消费者注入直呼四方法
            // 该组件住常驻的 Scene_MainMenu（Scene_Run 里没有），按本场景注册会抛 VContainerException，
            // 故由常驻编排者在 Build 之前经 BindPersistentServices 转交；缺失即装配错误，不做静默降级
            if (!_audioRoot || !_shakeDriver)
                throw new InvalidOperationException(
                    $"[{nameof(RunLifetimeScope)}] 常驻服务未转交（AudioRoot / ScreenShakeDriver），整局容器不构建");
            builder_.RegisterInstance(_audioRoot);
            builder_.RegisterInstance(_shakeDriver);
            // 整局 UI：跟随这一整局，进战斗后才有响应，段不在场时静默
            builder_.RegisterComponentInHierarchy<UI_RelicsGroup>();
            builder_.RegisterComponentInHierarchy<UI_PotionGroup>();
            builder_.RegisterComponentInHierarchy<UI_TopBar>(); // 装配完即自激活显示
            builder_.RegisterComponentInHierarchy<UI_DeckPanel>(); // 卡组查看：整副局内卡组的陈列，跑图/战斗段都随顶栏开关
            builder_.RegisterComponentInHierarchy<UI_MapView>(); // 地图编排器：装配时从 RunState 拿整章图一次性建视图
            // 墨迹控制器：注入视图以订阅 MapRebuilt，换局重建即清空本局笔迹；同时进存档集合
            builder_.RegisterComponentInHierarchy<UI_MapInk>().AsSelf().As<ISaveable>();
            // 段场景加载管理器：由它 additive 加载各段，段收尾后保留到选定下一个节点才卸载，它也要拿到会话句柄
            builder_.RegisterComponentInHierarchy<SegmentSceneManager>().AsSelf();
            builder_.RegisterComponentInHierarchy<UI_ResultPanel>(); // 整局终点结果面板（Canvas_System 顶层）：通关/战败共用，确认后上报回主菜单
            // 常驻发起器随整局容器装配：PopFactory 本就注册在这层，段不在场时悬停也能弹气泡；
            // 段内预摆/池化的实例由 BattleLifetimeScope 的构建回调兜底（整局层回调更早跑、那时段还没生成，漏不掉它们）
            builder_.RegisterBuildCallback(resolver_ =>
            {
                foreach (UI_HoverTip tip in FindObjectsOfType<UI_HoverTip>(true)) resolver_.Inject(tip);
                BindMainMenuFlow(resolver_);
                // 容器装配完成后一次性交接存档集合：读档快照此时统一下发，笔迹经 MapRebuilt 订阅链幂等重建
                SaveService.Bind(resolver_.Resolve<IReadOnlyList<ISaveable>>());
                if (SaveService.HasPendingRestore) SaveService.ApplyRestore();
                else SaveService.Save(); // 新局补初始档：先古段开始前即有档，退出后可继续
            });
        }

        // 容器建好后把段编排者的入口与常驻黑幕交给主菜单编排者：
        // MainMenuFlow 住常驻的 Scene_MainMenu、段编排者住 Scene_Run，跨场景引用不可序列化，故只拿委托
        private static void BindMainMenuFlow(IObjectResolver resolver_)
        {
            MainMenuFlow flow = FindFirstObjectByType<MainMenuFlow>();
            if (!flow) return; // DevPanel 等旁路直进 Scene_Run 时没有主菜单编排者：段流程照常，仅无黑幕与回主菜单出口
            SegmentSceneManager manager = resolver_.Resolve<SegmentSceneManager>();
            flow.BindRunHandlers(manager.StartSegmentAsync, manager.PrepareReturnToMenuAsync);
            manager.BindCamera(flow.MainCamera);
            manager.BindFadeScreen(flow.FadeScreen); // 黑屏唯一实例住 Scene_MainMenu：换段/卸段共用同一份幕布
            manager.BindReturnHandler(flow.RequestReturnToMainMenu);
            // 暂停面板的「设置」「放弃」也接常驻画布上的同一份面板（跨场景引用不可序列化，只能运行时转交）
            manager.BindAppPanels(flow.ShowSettingPanel, flow.ShowGiveUpPanel);
        }
    }
}
