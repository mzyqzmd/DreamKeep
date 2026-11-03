using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using _DreamKeep.CommonPresentation;
using _DreamKeep.ConfigData;
using _DreamKeep.GameRun;

namespace _DreamKeep.Presentation
{
    /// <summary>先古之民段组合根：从父容器取整局状态，用段配置装配出本次拜访的段模型并转发给表现入口。
    /// 拾起时结算要用到升级/移除两张选牌面板，两件都以场景件登记（用 FindObjectOfType 自取，含未激活节点）；
    /// autoRun 关、parentReference 由 SegmentSceneManager 运行时赋值（照战斗段先例）；
    /// 漏接线是装配期错误，直接抛异常暴露。</summary>
    public class AncientLifetimeScope : LifetimeScope
    {
        [SerializeField] private SO_Ancient _ancient;

        protected override void Configure(IContainerBuilder builder_)
        {
            if (!_ancient) throw new InvalidOperationException($"[{nameof(AncientLifetimeScope)}] SO_Ancient 未接线");
            if (!_ancient.RelicPool) throw new InvalidOperationException($"[{nameof(AncientLifetimeScope)}] SO_Ancient 遗物池未接线");

            RunState run = Parent.Container.Resolve<RunState>();
            builder_.RegisterInstance(run);
            builder_.RegisterInstance(new AncientSession(_ancient, run));
            builder_.RegisterComponentInHierarchy<UI_AncientDialoguePanel>();
            builder_.RegisterComponentInHierarchy<UI_AncientOptionGroup>();
            builder_.RegisterComponentInHierarchy<UI_AncientPopRoot>();
            builder_.RegisterComponentInHierarchy<UI_CardUpgradePanel>(); // 注册即注入：面板池与特效工厂走 [Inject] Construct
            builder_.RegisterComponentInHierarchy<UI_CardRemovePanel>();
            builder_.RegisterEntryPoint<AncientEntry>();

            // 段场景晚于整局容器加载：段内游离的气泡发起器（预摆特写卡、弹板子树等不在显式注入子树里的）在此兜底注入；
            // 已被注入过的实例重复跑一遍 [Inject] 只是把依赖再赋值一次，无副作用
            builder_.RegisterBuildCallback(resolver_ =>
            {
                foreach (UI_HoverTip tip in FindObjectsOfType<UI_HoverTip>(true)) resolver_.Inject(tip);
            });
        }
    }
}
