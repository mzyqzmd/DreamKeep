using System;
using _DreamKeep.CommonPresentation;
using _DreamKeep.ConfigData;
using _DreamKeep.GameRun;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _DreamKeep.Presentation
{
    /// <summary>商店段组合根：从父容器取整局状态，把八份池与价目表装配成会话，再装配出本段的事实广播器、
    /// 流程循环与命令门面；autoRun 关、parentReference 由 SegmentSceneLoader 运行时赋值。
    /// 池或价格漏接线是装配期错误，直接抛异常暴露。</summary>
    public class ShopLifetimeScope : LifetimeScope
    {
        [Header("卡池")]
        [Tooltip("普通卡池")] [SerializeField]
        private SO_CardPool _normalCards;

        [Tooltip("稀有卡池（与普通池合并成左上那一排）")] [SerializeField]
        private SO_CardPool _rareCards;

        [Tooltip("超稀有卡池（右上那一排）")] [SerializeField]
        private SO_CardPool _superRareCards;

        [Header("遗物池")]
        [Tooltip("普通遗物池：货架第一件")] [SerializeField]
        private SO_RelicPool _commonRelics;

        [Tooltip("稀有遗物池：货架第二件")] [SerializeField]
        private SO_RelicPool _rareRelics;

        [Tooltip("商店专属遗物池：货架第三件")] [SerializeField]
        private SO_RelicPool _shopRelics;

        [Header("药水池")]
        [Tooltip("普通药水池")] [SerializeField]
        private SO_PotionPool _commonPotions;

        [Tooltip("稀有药水池")] [SerializeField]
        private SO_PotionPool _rarePotions;

        [Header("价目表")]
        [Tooltip("各档位基础价与删牌涨价步进：半价与涨价由段模型按这里换算")] [SerializeField]
        private SO_ShopPrices _prices;

        protected override void Configure(IContainerBuilder builder_)
        {
            if (!_normalCards) throw new InvalidOperationException($"[{nameof(ShopLifetimeScope)}] 普通卡池未接线");
            if (!_rareCards) throw new InvalidOperationException($"[{nameof(ShopLifetimeScope)}] 稀有卡池未接线");
            if (!_superRareCards) throw new InvalidOperationException($"[{nameof(ShopLifetimeScope)}] 超稀有卡池未接线");
            if (!_commonRelics) throw new InvalidOperationException($"[{nameof(ShopLifetimeScope)}] 普通遗物池未接线");
            if (!_rareRelics) throw new InvalidOperationException($"[{nameof(ShopLifetimeScope)}] 稀有遗物池未接线");
            if (!_shopRelics) throw new InvalidOperationException($"[{nameof(ShopLifetimeScope)}] 商店遗物池未接线");
            if (!_commonPotions) throw new InvalidOperationException($"[{nameof(ShopLifetimeScope)}] 普通药水池未接线");
            if (!_rarePotions) throw new InvalidOperationException($"[{nameof(ShopLifetimeScope)}] 稀有药水池未接线");
            if (!_prices) throw new InvalidOperationException($"[{nameof(ShopLifetimeScope)}] 价目表未接线");

            RunState run = Parent.Container.Resolve<RunState>();
            ShopSession session = new ShopSession(run, _prices, _normalCards, _rareCards, _superRareCards,
                _commonRelics, _rareRelics, _shopRelics, _commonPotions, _rarePotions);
            ShopEventDispatcher events = new ShopEventDispatcher();
            ShopLoop loop = new ShopLoop(session, run, events, Parent.Container.Resolve<RelicTriggerNotifier>());

            builder_.RegisterInstance(run);
            builder_.RegisterInstance(events); // 供视图 [Inject] 注册事实处理器
            builder_.RegisterInstance(loop);
            builder_.RegisterInstance(new ShopCommandDispatcher(loop)); // 供视图提交意图
            builder_.RegisterComponentInHierarchy<UI_ShopRugPanel>();
            builder_.RegisterComponentInHierarchy<UI_ShopMerchant>();
            builder_.RegisterComponentInHierarchy<UI_ShopProceedItem>();
            builder_.RegisterComponentInHierarchy<UI_CardRemovePanel>(); // 场景里已摆好的删牌弹板实例
            builder_.RegisterEntryPoint<ShopEntry>();

            // 段场景晚于整局容器加载：段内游离的气泡发起器在此兜底注入；重复注入只是重赋依赖，无副作用
            builder_.RegisterBuildCallback(resolver_ =>
            {
                foreach (UI_HoverTip tip in FindObjectsOfType<UI_HoverTip>(true)) resolver_.Inject(tip);
            });
        }
    }
}
