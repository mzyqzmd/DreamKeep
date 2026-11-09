using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using _DreamKeep.CommonPresentation;
using _DreamKeep.ConfigData;
using _DreamKeep.GameRun;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Presentation
{
    /// <summary>事件段组合根：从父容器取整局状态，用事件资产装配出本段的规则模型、事实广播器与流程循环，
    /// 命令门面交给视图提交意图；autoRun 关、parentReference 由 SegmentSceneLoader 运行时赋值。
    /// 漏接线是装配期错误，直接抛异常暴露。</summary>
    public class EventLifetimeScope : LifetimeScope
    {
        [Tooltip("本批已实装的事件资产：进入事件段时随机掷一则")]
        [SerializeField] private SO_Event[] _events;

        protected override void Configure(IContainerBuilder builder_)
        {
            if (_events == null || _events.Length == 0)
                throw new InvalidOperationException($"[{nameof(EventLifetimeScope)}] 事件资产列表未接线");
            Validate(_events);

            RunState run = Parent.Container.Resolve<RunState>();
            EventEventDispatcher events = new EventEventDispatcher();
            EventLoop loop = new EventLoop(new EventSession(run, _events), run, events);

            builder_.RegisterInstance(run);
            builder_.RegisterInstance(events); // 供视图 [Inject] 注册事实处理器
            builder_.RegisterInstance(loop);
            builder_.RegisterInstance(new EventCommandDispatcher(loop)); // 供视图提交意图
            builder_.RegisterComponentInHierarchy<UI_EventPanel>();
            builder_.RegisterComponentInHierarchy<UI_EventOptionGroup>();
            builder_.RegisterComponentInHierarchy<UI_RewardPanel>();
            builder_.RegisterComponentInHierarchy<UI_CardRemovePanel>();
            builder_.RegisterComponentInHierarchy<UI_CardUpgradePanel>();
            builder_.RegisterEntryPoint<EventEntry>();

            // 段场景晚于整局容器加载：段内游离的气泡发起器（预摆特写卡、弹板子树等不在显式注入子树里的）在此兜底注入；
            // 已被注入过的实例重复跑一遍 [Inject] 只是把依赖再赋值一次，无副作用
            builder_.RegisterBuildCallback(resolver_ =>
            {
                foreach (UI_HoverTip tip in FindObjectsOfType<UI_HoverTip>(true)) resolver_.Inject(tip);
            });
        }

        // 装配期体检：效果里的断引用要等玩家点选才 NRE，这里提前炸出并带事件名与选项名
        private static void Validate(SO_Event[] events_)
        {
            for (int e = 0; e < events_.Length; e++)
            {
                SO_Event config = events_[e];
                if (config == null)
                    throw new InvalidOperationException($"[{nameof(EventLifetimeScope)}] 事件列表第 {e} 项是空引用");

                for (int o = 0; o < config.Options.Count; o++)
                {
                    EventOptionData option = config.Options[o];
                    for (int i = 0; i < option.Effects.Count; i++)
                    {
                        string missing = option.Effects[i] switch
                        {
                            EED_AddCard addCard when addCard.Card == null => "卡牌",
                            EED_RandomRelic randomRelic when randomRelic.Pool == null => "遗物池",
                            EED_RelicChoice relicChoice when relicChoice.Pool == null => "遗物池",
                            EED_RewardPotions rewardPotions when rewardPotions.Pool == null => "药水池",
                            _ => null,
                        };
                        if (missing != null)
                            throw new InvalidOperationException(
                                $"[{nameof(EventLifetimeScope)}] 事件「{config.EventName}」选项「{option.Name}」的第 {i} 个效果缺少{missing}引用");
                    }
                }
            }
        }
    }
}
