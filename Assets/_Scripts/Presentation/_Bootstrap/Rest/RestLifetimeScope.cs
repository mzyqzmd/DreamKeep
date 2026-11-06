using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using _DreamKeep.CommonPresentation;
using _DreamKeep.ConfigData;
using _DreamKeep.GameRun;
using _DreamKeep.Presentation.Root;

namespace _DreamKeep.Presentation
{
    /// <summary>休息段组合根：从父容器取整局状态，用选项资产与挖掘候选池装配出本段的事实广播器与流程循环，
    /// 命令门面交给视图提交意图；autoRun 关、parentReference 由 SegmentSceneLoader 运行时赋值。
    /// 漏接线是装配期错误，直接抛异常暴露。</summary>
    public class RestLifetimeScope : LifetimeScope
    {
        [Header("挖掘候选池")]
        [Tooltip("挖掘候选：Common 池（与 Rare 池合并后过滤已持有）")]
        [SerializeField] private SO_RelicPool _commonPool;
        [Tooltip("挖掘候选：Rare 池（与 Common 池合并后过滤已持有）")]
        [SerializeField] private SO_RelicPool _rarePool;

        [Header("休息选项")]
        [Tooltip("选项资产列表：顺序即选项出现顺序")]
        [SerializeField] private List<SO_RestOption> _restOptions;

        protected override void Configure(IContainerBuilder builder_)
        {
            RunState run = Parent.Container.Resolve<RunState>();
            ValidateOptions(run);

            RestEventDispatcher events = new RestEventDispatcher();
            RestLoop loop = new RestLoop(run, _restOptions, _commonPool, _rarePool,
                Parent.Container.Resolve<RelicTriggerNotifier>(), events);

            builder_.RegisterInstance(run);
            builder_.RegisterInstance(events); // 供视图 [Inject] 注册事实处理器
            builder_.RegisterInstance(loop);
            builder_.RegisterInstance(new RestCommandDispatcher(loop)); // 供视图提交意图
            builder_.RegisterComponentInHierarchy<UI_RestOptionGroup>(); // 注册即注入：选项组要订阅事实与拿命令门面
            builder_.RegisterInstance(Require<UI_RestVFXRoot>());
            builder_.RegisterComponentInHierarchy<UI_CardUpgradePanel>(); // 注册即注入：面板池与特效工厂走 [Inject] Construct
            builder_.RegisterInstance(Require<UI_RestProceedButton>());
            builder_.RegisterEntryPoint<RestEntry>();

            // 段场景晚于整局容器加载：段内游离的气泡发起器在此兜底注入；重复注入只是重赋依赖，无副作用
            builder_.RegisterBuildCallback(resolver_ =>
            {
                foreach (UI_HoverTip tip in FindObjectsOfType<UI_HoverTip>(true)) resolver_.Inject(tip);
            });
        }

        // 选项清单装配期体检：非空、无空引用、类型已登记；持有切肉刀/铲子却缺对应资格选项也在此暴露
        private void ValidateOptions(RunState run_)
        {
            if (_restOptions == null || _restOptions.Count == 0)
                throw new InvalidOperationException($"[{nameof(RestLifetimeScope)}] 休息选项列表未接线");
            for (int i = 0; i < _restOptions.Count; i++)
                if (!_restOptions[i]) throw new InvalidOperationException($"[{nameof(RestLifetimeScope)}] 休息选项第 {i} 项为空引用");

            if (_restOptions.Any(o => o is not SO_HealRestOption and not SO_ForgeRestOption and not SO_CookRestOption and not SO_DigRestOption))
                throw new InvalidOperationException($"[{nameof(RestLifetimeScope)}] 休息选项含未登记的类型，段入口不知道怎么结算");

            if (run_.Player.Relics.Any(static r => r is SO_MeatCleaver) && _restOptions.All(static o => o is not SO_CookRestOption))
                throw new InvalidOperationException($"[{nameof(RestLifetimeScope)}] 玩家持有切肉刀，但选项列表没有烹饪");
            if (run_.Player.Relics.Any(static r => r is SO_Shovel) && _restOptions.All(static o => o is not SO_DigRestOption))
                throw new InvalidOperationException($"[{nameof(RestLifetimeScope)}] 玩家持有铲子，但选项列表没有挖掘");
        }

        private static T Require<T>() where T : Component
        {
            T found = UnityEngine.Object.FindObjectOfType<T>(true);
            if (found == null) throw new InvalidOperationException($"[{nameof(RestLifetimeScope)}] 场景缺少 {typeof(T).Name}");
            return found;
        }
    }
}
