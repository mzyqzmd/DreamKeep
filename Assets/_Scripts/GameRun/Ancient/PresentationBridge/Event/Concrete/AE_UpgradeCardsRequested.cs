namespace _DreamKeep.GameRun
{
    /// <summary>香囊代价：强制选牌升级，关掉取消许可；选中列表或取消经命令门面交回。</summary>
    public struct AE_UpgradeCardsRequested : IAncientEvent
    {
        public int Count { get; }

        public AE_UpgradeCardsRequested(int count_) => Count = count_;
    }
}
