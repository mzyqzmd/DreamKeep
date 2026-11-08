using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>购买被拒（金币不足/药水满槽/已售出）：视图对刚点的那一格抖动一次，
    /// 并让商人按原因说一句（SoldOut 属点击竞态，不发声）。</summary>
    public struct SE_PurchaseRefused : IShopEvent
    {
        public EShopBuyResult Reason { get; }

        public SE_PurchaseRefused(EShopBuyResult reason_) => Reason = reason_;
    }
}
