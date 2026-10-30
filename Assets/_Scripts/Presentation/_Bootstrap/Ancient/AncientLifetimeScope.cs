using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using _DreamKeep.ConfigData;
using _DreamKeep.GameRun;

namespace _DreamKeep.Presentation
{
    /// <summary>先古之民段组合根：从父容器取整局状态，用段配置装配出本次拜访的段模型并转发给表现入口。
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
            builder_.RegisterInstance(new AncientSession(_ancient, run));
            builder_.RegisterComponentInHierarchy<UI_AncientDialoguePanel>();
            builder_.RegisterComponentInHierarchy<UI_AncientOptionGroup>();
            builder_.RegisterComponentInHierarchy<UI_AncientPopRoot>();
            builder_.RegisterEntryPoint<AncientEntry>();
        }
    }
}
