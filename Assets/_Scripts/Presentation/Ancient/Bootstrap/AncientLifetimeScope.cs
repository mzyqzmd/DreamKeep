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
    /// <summary>先古之民段组合根：从父容器取整局状态，用段配置装配出本次拜访的会话、事实广播器与流程循环，
    /// 命令门面交给视图提交意图；autoRun 关、parentReference 由 SegmentSceneLoader 运行时赋值。
    /// 漏接线是装配期错误，直接抛异常暴露。</summary>
    public class AncientLifetimeScope : LifetimeScope
    {
        [SerializeField] private SO_Ancient _ancient;

        protected override void Configure(IContainerBuilder builder_)
        {
            if (!_ancient) throw new InvalidOperationException($"[{nameof(AncientLifetimeScope)}] SO_Ancient 未接线");
            if (!_ancient.RelicPool) throw new InvalidOperationException($"[{nameof(AncientLifetimeScope)}] SO_Ancient 遗物池未接线");

            RunState run = Parent.Container.Resolve<RunState>();
            AncientEventDispatcher events = new AncientEventDispatcher();
            AncientLoop loop = new AncientLoop(new AncientSession(_ancient, run), run, events,
                Parent.Container.Resolve<RelicTriggerNotifier>());

            builder_.RegisterInstance(run);
            builder_.RegisterInstance(events); // 供视图 [Inject] 注册事实处理器
            builder_.RegisterInstance(loop);
            builder_.RegisterInstance(new AncientCommandDispatcher(loop)); // 供视图提交意图
            builder_.RegisterComponentInHierarchy<UI_AncientDialoguePanel>();
            builder_.RegisterComponentInHierarchy<UI_AncientOptionGroup>();
            builder_.RegisterComponentInHierarchy<UI_AncientPopRoot>();
            builder_.RegisterComponentInHierarchy<UI_CardUpgradePanel>(); // 注册即注入：面板池与特效工厂走 [Inject] Construct
            builder_.RegisterComponentInHierarchy<UI_CardRemovePanel>();
            builder_.RegisterEntryPoint<AncientEntry>();

            // 段场景晚于整局容器加载：段内游离的气泡发起器在此兜底注入；重复注入只是重赋依赖，无副作用
            builder_.RegisterBuildCallback(resolver_ =>
            {
                foreach (UI_HoverTip tip in FindObjectsOfType<UI_HoverTip>(true)) resolver_.Inject(tip);
            });
        }
    }
}
