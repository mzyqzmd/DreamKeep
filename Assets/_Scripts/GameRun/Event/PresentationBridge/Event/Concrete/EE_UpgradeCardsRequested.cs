namespace _DreamKeep.GameRun
{
    /// <summary>请求玩家自选升级卡牌：视图弹升级板，选中列表或取消经命令门面交回。</summary>
    public struct EE_UpgradeCardsRequested : IEventSegmentEvent
    {
        public int Count { get; }

        public EE_UpgradeCardsRequested(int count_) => Count = count_;
    }
}
