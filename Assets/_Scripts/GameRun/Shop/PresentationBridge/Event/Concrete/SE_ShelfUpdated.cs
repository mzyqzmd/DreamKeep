namespace _DreamKeep.GameRun
{
    /// <summary>货架与价格就位：进店首次绑定（Initial）或成交后重染，视图按快照铺格子与染色。</summary>
    public struct SE_ShelfUpdated : IShopEvent
    {
        public ShopShelfSnapshot Shelf { get; }
        public bool Initial { get; }

        public SE_ShelfUpdated(ShopShelfSnapshot shelf_, bool initial_)
        {
            Shelf = shelf_;
            Initial = initial_;
        }
    }
}
