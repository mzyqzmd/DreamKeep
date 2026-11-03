using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using _DreamKeep.CommonPresentation;
using _DreamKeep.ConfigData;
using _DreamKeep.GameRun;

namespace _DreamKeep.Presentation
{
    /// <summary>宝箱段组合根：从父容器取整局状态，用两个遗物池装配出本次开启的段模型并转发给表现入口。
    /// autoRun 关、parentReference 由 SegmentSceneManager 运行时赋值（照先古段先例）；
    /// 漏接线是装配期错误，直接抛异常暴露。</summary>
    public class ChestLifetimeScope : LifetimeScope
    {
        [SerializeField] private SO_RelicPool _commonPool;
        [SerializeField] private SO_RelicPool _rarePool;

        protected override void Configure(IContainerBuilder builder_)
        {
            if (!_commonPool) throw new InvalidOperationException($"[{nameof(ChestLifetimeScope)}] 普通遗物池未接线");
            if (!_rarePool) throw new InvalidOperationException($"[{nameof(ChestLifetimeScope)}] 稀有遗物池未接线");

            RunState run = Parent.Container.Resolve<RunState>();
            builder_.RegisterInstance(new ChestSession(_commonPool, _rarePool, run));
            builder_.RegisterComponentInHierarchy<UI_Chest>();
            builder_.RegisterComponentInHierarchy<UI_RelicSelectPanel>();
            builder_.RegisterComponentInHierarchy<UI_ChestProceedButton>();
            builder_.RegisterEntryPoint<ChestEntry>();

            // 段场景晚于整局容器加载：段内游离的气泡发起器（预摆特写卡、弹板子树等不在显式注入子树里的）在此兜底注入；
            // 已被注入过的实例重复跑一遍 [Inject] 只是把依赖再赋值一次，无副作用
            builder_.RegisterBuildCallback(resolver_ =>
            {
                foreach (UI_HoverTip tip in FindObjectsOfType<UI_HoverTip>(true)) resolver_.Inject(tip);
            });
        }
    }
}
