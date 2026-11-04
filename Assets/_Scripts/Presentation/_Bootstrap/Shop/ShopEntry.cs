using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.CommonPresentation;
using _DreamKeep.ConfigData;
using _DreamKeep.GameRun;
using _DreamKeep.RuntimeData;
using VContainer;
using VContainer.Unity;

namespace _DreamKeep.Presentation
{
    /// <summary>商店段入口：本段"从哪里开始"的唯一落点——进店先收地毯与「继续」，把货架绑好；
    /// 玩家点商人掀开地毯，首次掀开时亮出「继续」（此后常驻）；「继续」每次点击都开地图并保留本段。
    /// 删牌服务在这里做金币校验并驱动删牌弹板：关板取消不扣钱，确认后的删牌由弹板自己做、扣款与计数由模型记。</summary>
    public sealed class ShopEntry : IStartable
    {
        private readonly RunState _run;
        private readonly ShopSession _session;
        private readonly UI_ShopRugPanel _rug;
        private readonly UI_ShopMerchant _merchant;
        private readonly UI_ShopProceedItem _proceedButton;
        private readonly UI_CardRemovePanel _removePanel;
        private readonly RelicTriggerNotifier _notifier;
        private readonly IObjectResolver _resolver;

        private bool _proceedShown;

        public ShopEntry(RunState run_, ShopSession session_, UI_ShopRugPanel rug_, UI_ShopMerchant merchant_,
            UI_ShopProceedItem proceedButton_, UI_CardRemovePanel removePanel_, RelicTriggerNotifier notifier_,
            IObjectResolver resolver_)
        {
            _run = run_;
            _session = session_;
            _rug = rug_;
            _merchant = merchant_;
            _proceedButton = proceedButton_;
            _removePanel = removePanel_;
            _notifier = notifier_;
            _resolver = resolver_;
        }

        public void Start()
        {
            // 地毯子树的悬停气泡要靠 PopFactory：场景件只接了地毯本体，子树在这里补一次注入（Bind 前必须完成）
            _resolver.InjectGameObject(_rug.gameObject);

            _rug.Bind(_session);
            _rug.Hide(); // 进店先收摊：玩家点商人再掀开

            _rug.RelicBought += OnRelicBought;
            _rug.RemovalRequested += OnRemovalRequested;
            _merchant.Clicked += OnMerchantClicked;

            // 删牌弹板是跨段复用的单例：先摘再挂，避免重复开板时累积
            _removePanel.Confirmed -= OnRemovalConfirmed;
            _removePanel.Confirmed += OnRemovalConfirmed;
        }

        // 点商人：掀开地毯（从上往下滑入）；首次掀开顺手亮出「继续」，此后常驻（关掉地毯也留着）
        private void OnMerchantClicked()
        {
            if (_rug.gameObject.activeSelf) return;
            _rug.PlayOpenAsync().Forget();
            _run.CompleteMapNode(); // 首次掀开摊子即通过（幂等），不等玩家点「继续」
            if (_proceedShown) return;
            _proceedShown = true;
            _proceedButton.Show();
        }

        // 玩家在删牌弹板按下确认的同一刻：删牌服务位播一次翻面并进入不可用态
        private void OnRemovalConfirmed() => _rug.PlayRemovalFlip();

        // 买到遗物：只播一次涟漪，遗物的战斗内效果不进商店
        private void OnRelicBought(SO_Relic relic_) => _notifier.NotifyTriggered(relic_);

        private void OnRemovalRequested() => RemoveCardAsync().Forget();

        // 删牌服务：先校验金币再弹板（买不起不弹，免得白选一轮），确认后扣款并置灰
        private async UniTask RemoveCardAsync()
        {
            EShopBuyResult check = _session.CheckRemoval();
            if (check == EShopBuyResult.SoldOut) return;
            if (check == EShopBuyResult.NotEnoughGold)
            {
                _rug.PlayRemovalRefuse();
                return;
            }

            _removePanel.Show(_run.Player, 1);
            IReadOnlyList<CardModel> removed = await _removePanel.WaitResultAsync();
            if (removed == null) return; // 关板取消：不扣钱、价格不动

            _session.CompleteRemoval();
            _rug.RefreshPrices();
        }
    }
}
