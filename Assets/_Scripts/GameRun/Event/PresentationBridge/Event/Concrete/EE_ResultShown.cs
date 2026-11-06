namespace _DreamKeep.GameRun
{
    /// <summary>切到第 2 页：只换结果文案，标题与背景保持不动。</summary>
    public struct EE_ResultShown : IEventSegmentEvent
    {
        public string ResultText { get; }

        public EE_ResultShown(string resultText_) => ResultText = resultText_;
    }
}
