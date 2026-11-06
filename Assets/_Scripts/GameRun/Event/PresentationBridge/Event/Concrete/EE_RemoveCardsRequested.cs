namespace _DreamKeep.GameRun
{
    /// <summary>请求玩家自选移除卡牌：视图弹删牌板，选中列表或取消经命令门面交回。</summary>
    public struct EE_RemoveCardsRequested : IEventSegmentEvent
    {
        public int Count { get; }

        public EE_RemoveCardsRequested(int count_) => Count = count_;
    }
}
