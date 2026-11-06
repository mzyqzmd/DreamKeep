namespace _DreamKeep.GameRun
{
    /// <summary>烹饪自选请求：视图弹删牌板，选中列表或取消经命令门面交回。</summary>
    public struct REE_RemoveCardsRequested : IRestEvent
    {
        public int Count { get; }

        public REE_RemoveCardsRequested(int count_) => Count = count_;
    }
}
