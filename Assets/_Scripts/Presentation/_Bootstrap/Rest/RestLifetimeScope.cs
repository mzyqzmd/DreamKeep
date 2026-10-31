using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using _DreamKeep.ConfigData;
using _DreamKeep.GameRun;
using _DreamKeep.Presentation.Root;

namespace _DreamKeep.Presentation
{
    /// <summary>休息段组合根：从父容器取整局状态，把场景里的表现组件显式登记进段容器并启动段入口。
    /// 用 FindObjectOfType(true) 自取（含未激活节点，面板与继续按钮初始是关的），漏接线装配期直接抛异常暴露。
    /// 挖掘候选池与选项图标由本组合根序列化资产后 Keyed/整体注入；autoRun 关、parentReference 由 SegmentSceneManager 运行时赋值。</summary>
    public class RestLifetimeScope : LifetimeScope
    {
        [Header("挖掘候选池")]
        [Tooltip("挖掘候选：Common 池（与 Rare 池合并后过滤已持有）")]
        [SerializeField] private SO_RelicPool _commonPool;
        [Tooltip("挖掘候选：Rare 池（与 Common 池合并后过滤已持有）")]
        [SerializeField] private SO_RelicPool _rarePool;

        [Header("选项图标")]
        [Tooltip("休息选项图标（option_heal）")]
        [SerializeField] private Sprite _healIcon;
        [Tooltip("锻造选项图标（option_smith）")]
        [SerializeField] private Sprite _smithIcon;
        [Tooltip("挖掘选项图标（option_dig）")]
        [SerializeField] private Sprite _digIcon;

        protected override void Configure(IContainerBuilder builder_)
        {
            RunState run = Parent.Container.Resolve<RunState>();
            builder_.RegisterInstance(run);
            builder_.RegisterInstance(Require<UI_RestOptionGroup>());
            builder_.RegisterInstance(Require<UI_RestVFXRoot>());
            builder_.RegisterInstance(Require<UI_SmithPanel>());
            builder_.RegisterInstance(Require<UI_RestProceed>());
            builder_.RegisterInstance(_commonPool).Keyed("Common");
            builder_.RegisterInstance(_rarePool).Keyed("Rare");
            builder_.RegisterInstance(new RestEntry.OptionIcons(_healIcon, _smithIcon, _digIcon));
            builder_.RegisterEntryPoint<RestEntry>();
        }

        private static T Require<T>() where T : Component
        {
            T found = UnityEngine.Object.FindObjectOfType<T>(true);
            if (found == null) throw new InvalidOperationException($"[{nameof(RestLifetimeScope)}] 场景缺少 {typeof(T).Name}");
            return found;
        }
    }
}
