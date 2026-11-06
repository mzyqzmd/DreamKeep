using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>单条事件选项视图模型：标题、副标题（置灰时=原因文案）、是否可点、悬停展示卡（可空）。</summary>
    public readonly struct EventOptionViewModel
    {
        public string Title { get; }
        public string Subtitle { get; }
        public bool Enabled { get; }
        public CardModel DisplayCard { get; }

        public EventOptionViewModel(string title_, string subtitle_, bool enabled_, CardModel displayCard_)
        {
            Title = title_;
            Subtitle = subtitle_;
            Enabled = enabled_;
            DisplayCard = displayCard_;
        }
    }
}
