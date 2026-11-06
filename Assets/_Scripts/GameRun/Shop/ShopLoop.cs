using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>商店段流程：货架快照经商店段分发器播出，购买与删牌意图由命令门面同步结算后回播新快照。
    /// 首次掀开摊子即标记节点通过（幂等）；成交失败只播被拒事实，不改货架；删牌取消不扣款。
    /// 纯 C#，不认识场景与视图；多次往返交易不需要等待点。</summary>
    public sealed class ShopLoop
    {
        private readonly ShopSession _session;
        private readonly RunState _run;
        private readonly ShopEventDispatcher _events;
        private readonly RelicTriggerNotifier _notifier;

        public ShopLoop(ShopSession session_, RunState run_, ShopEventDispatcher events_, RelicTriggerNotifier notifier_)
        {
            _session = session_;
            _run = run_;
            _events = events_;
            _notifier = notifier_;
        }

        public void Start() => _events.PlayAsync(new SE_ShelfUpdated(_session.BuildSnapshot(), true)).Forget();

        public void OpenShop()
        {
            _run.CompleteMapNode(); // 首次掀开摊子即通过（幂等），不等玩家点「继续」
            _events.PlayAsync(new SE_ShopOpened()).Forget();
        }

        public void BuyCard(CardModel card_)
        {
            if (_session.TryBuyCard(card_) != EShopBuyResult.Success) Refuse();
            else PublishShelf();
        }

        public void BuyRelic(SO_Relic relic_)
        {
            if (_session.TryBuyRelic(relic_) != EShopBuyResult.Success) Refuse();
            else
            {
                _notifier.NotifyTriggered(relic_); // 只播涟漪，不触发遗物效果
                PublishShelf();
            }
        }

        public void BuyPotion(SO_Potion potion_)
        {
            if (_session.TryBuyPotion(potion_) != EShopBuyResult.Success) Refuse();
            else PublishShelf();
        }

        public void RequestRemoval()
        {
            EShopBuyResult check = _session.CheckRemoval();
            if (check == EShopBuyResult.SoldOut) return;
            if (check == EShopBuyResult.NotEnoughGold)
            {
                _events.PlayAsync(new SE_RemovalRefused()).Forget();
                return;
            }

            _events.PlayAsync(new SE_RemovalPanelRequested(1)).Forget();
        }

        public void SubmitCardSelection(IReadOnlyList<CardModel> cards_)
        {
            if (cards_ == null) return; // 关板取消：不扣钱、价格不动
            _session.CompleteRemoval();
            PublishShelf();
        }

        public void Proceed() => _events.PlayAsync(new SE_ShopCompleted()).Forget();

        private void PublishShelf() => _events.PlayAsync(new SE_ShelfUpdated(_session.BuildSnapshot(), false)).Forget();

        private void Refuse() => _events.PlayAsync(new SE_PurchaseRefused()).Forget();
    }
}
