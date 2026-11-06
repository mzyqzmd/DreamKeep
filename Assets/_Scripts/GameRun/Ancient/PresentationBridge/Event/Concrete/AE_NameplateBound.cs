namespace _DreamKeep.GameRun
{
    /// <summary>名号就位：对话面板写名号牌、弹层宿主播一次横幅，两个订阅者共用同一条事实。</summary>
    public struct AE_NameplateBound : IAncientEvent
    {
        public string AncientName { get; }
        public string PositionName { get; }

        public AE_NameplateBound(string ancientName_, string positionName_)
        {
            AncientName = ancientName_;
            PositionName = positionName_;
        }
    }
}
