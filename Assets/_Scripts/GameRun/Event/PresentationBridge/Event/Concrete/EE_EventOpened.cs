using _DreamKeep.ConfigData;

namespace _DreamKeep.GameRun
{
    /// <summary>事件开启：第 1 页陈列，视图据此写标题、叙事与背景。</summary>
    public struct EE_EventOpened : IEventSegmentEvent
    {
        public SO_Event Config { get; }

        public EE_EventOpened(SO_Event config_) => Config = config_;
    }
}
