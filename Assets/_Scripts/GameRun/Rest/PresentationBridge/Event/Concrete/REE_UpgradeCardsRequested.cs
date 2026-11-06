namespace _DreamKeep.GameRun
{
    /// <summary>锻造自选请求：视图弹升级板，选中列表或取消经命令门面交回。</summary>
    public struct REE_UpgradeCardsRequested : IRestEvent
    {
        public int Count { get; }

        public REE_UpgradeCardsRequested(int count_) => Count = count_;
    }
}
