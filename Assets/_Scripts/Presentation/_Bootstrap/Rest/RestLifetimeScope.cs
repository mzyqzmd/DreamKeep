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
    /// <summary>休息段组合根：从父容器取整局状态，把场景里的表现组件与休息选项资产显式登记进段容器并启动段入口。
    /// 用 FindObjectOfType(true) 自取（含未激活节点，面板与继续按钮初始是关的），漏接线装配期直接抛异常暴露；
    /// 挖掘候选池由本组合根序列化资产后 Keyed 注入；autoRun 关、parentReference 由 SegmentSceneManager 运行时赋值。</summary>
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

            builder_.RegisterInstance(run);
            builder_.RegisterInstance(Require<UI_RestOptionGroup>());
            builder_.RegisterInstance(Require<UI_RestVFXRoot>());
            builder_.RegisterComponentInHierarchy<UI_CardUpgradePanel>(); // 注册即注入：面板池与特效工厂走 [Inject] Construct
            builder_.RegisterInstance(Require<UI_RestProceedButton>());
            builder_.RegisterInstance(_commonPool).Keyed("Common");
            builder_.RegisterInstance(_rarePool).Keyed("Rare");
            builder_.RegisterInstance(_restOptions);
            builder_.RegisterEntryPoint<RestEntry>();

            // 段场景晚于整局容器加载：段内游离的气泡发起器（预摆特写卡、弹板子树等不在显式注入子树里的）在此兜底注入；
            // 已被注入过的实例重复跑一遍 [Inject] 只是把依赖再赋值一次，无副作用
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

            if (_restOptions.Any(o => o is not SO_RestOption_Heal and not SO_RestOption_Forge and not SO_RestOption_Cook and not SO_RestOption_Dig))
                throw new InvalidOperationException($"[{nameof(RestLifetimeScope)}] 休息选项含未登记的类型，段入口不知道怎么结算");

            if (run_.Player.Relics.Any(static r => r is SO_MeatCleaver) && _restOptions.All(static o => o is not SO_RestOption_Cook))
                throw new InvalidOperationException($"[{nameof(RestLifetimeScope)}] 玩家持有切肉刀，但选项列表没有烹饪");
            if (run_.Player.Relics.Any(static r => r is SO_Shovel) && _restOptions.All(static o => o is not SO_RestOption_Dig))
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
