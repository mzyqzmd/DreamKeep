namespace _DreamKeep.GameRun
{
    /// <summary>剪刀代价：强制选牌移除，关掉取消许可；选中列表或取消经命令门面交回。</summary>
    public struct AE_RemoveCardsRequested : IAncientEvent
    {
        public int Count { get; }

        public AE_RemoveCardsRequested(int count_) => Count = count_;
    }
}
