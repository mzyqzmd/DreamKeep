using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>商店段上行命令门面：视图只提交语义化意图，不直接触碰会话与流程。</summary>
    public sealed class ShopCommandDispatcher
    {
        private readonly ShopLoop _loop;

        public ShopCommandDispatcher(ShopLoop loop_) => _loop = loop_;

        /// <summary>点商人掀开货摊。</summary>
        public void OpenShop() => _loop.OpenShop();

        public void BuyCard(CardModel card_) => _loop.BuyCard(card_);

        public void BuyRelic(SO_Relic relic_) => _loop.BuyRelic(relic_);

        public void BuyPotion(SO_Potion potion_) => _loop.BuyPotion(potion_);

        /// <summary>点删牌服务位：金币校验与弹板由流程决定。</summary>
        public void RequestRemoval() => _loop.RequestRemovalAsync().Forget();

        /// <summary>点「继续」：每次点击都播一次段完成。</summary>
        public void Proceed() => _loop.Proceed();
    }
}
