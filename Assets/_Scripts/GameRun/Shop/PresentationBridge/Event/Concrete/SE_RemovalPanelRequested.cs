namespace _DreamKeep.GameRun
{
    /// <summary>删牌服务已校验通过：请求表现层弹删牌板，选中列表或取消经命令门面交回。</summary>
    public struct SE_RemovalPanelRequested : IShopEvent
    {
        public int Count { get; }

        public SE_RemovalPanelRequested(int count_) => Count = count_;
    }
}
