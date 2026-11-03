using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using _DreamKeep.CommonPresentation;
using _DreamKeep.ConfigData;
using _DreamKeep.GameRun;

namespace _DreamKeep.Presentation
{
    /// <summary>事件段组合根：从父容器取整局状态，把场景里的面板与选项组显式登记进段容器并启动段入口。
    /// 用 FindObjectOfType(true) 自取（含未激活节点，各面板初始是关的），漏接线装配期直接抛异常暴露；
    /// autoRun 关、parentReference 由 SegmentSceneManager 运行时赋值（照休息段先例）。</summary>
    public class EventLifetimeScope : LifetimeScope
    {
        [Tooltip("本批已实装的事件资产：进入事件段时随机掷一则")]
        [SerializeField] private SO_Event[] _events;

        protected override void Configure(IContainerBuilder builder_)
        {
            if (_events == null || _events.Length == 0)
                throw new InvalidOperationException($"[{nameof(EventLifetimeScope)}] 事件资产列表未接线");
            Validate(_events);

            UI_EventPanel panel = Require<UI_EventPanel>();
            UI_EventOptionGroup options = Require<UI_EventOptionGroup>();
            UI_RewardPanel rewardPanel = Require<UI_RewardPanel>();
            UI_CardRemovePanel removePanel = Require<UI_CardRemovePanel>();
            UI_CardUpgradePanel upgradePanel = Require<UI_CardUpgradePanel>();

            // 场景件走 RegisterComponentInHierarchy：注册即注入（[Inject] 成员照常跑），
            // RegisterInstance 不会自动注入——组的 IObjectResolver / 奖励面板的 Construct 会拿不到依赖
            RunState run = Parent.Container.Resolve<RunState>();
            builder_.RegisterInstance(run);
            builder_.RegisterComponentInHierarchy<UI_EventPanel>();
            builder_.RegisterComponentInHierarchy<UI_EventOptionGroup>();
            builder_.RegisterComponentInHierarchy<UI_RewardPanel>();
            builder_.RegisterComponentInHierarchy<UI_CardRemovePanel>();
            builder_.RegisterComponentInHierarchy<UI_CardUpgradePanel>();
            builder_.RegisterInstance(_events);
            builder_.RegisterInstance(new EventSession(run, _events, panel, options, rewardPanel, removePanel, upgradePanel));
            builder_.RegisterEntryPoint<EventEntry>();

            // 段场景晚于整局容器加载：段内游离的气泡发起器（预摆特写卡、弹板子树等不在显式注入子树里的）在此兜底注入；
            // 已被注入过的实例重复跑一遍 [Inject] 只是把依赖再赋值一次，无副作用
            builder_.RegisterBuildCallback(resolver_ =>
            {
                foreach (UI_HoverTip tip in FindObjectsOfType<UI_HoverTip>(true)) resolver_.Inject(tip);
            });
        }

        private T Require<T>() where T : Component
        {
            T found = FindObjectOfType<T>(true);
            if (found == null) throw new InvalidOperationException($"[{nameof(EventLifetimeScope)}] 场景缺少 {typeof(T).Name}");
            return found;
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
