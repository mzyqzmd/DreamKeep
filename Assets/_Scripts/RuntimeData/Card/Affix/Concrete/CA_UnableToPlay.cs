using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>不能被打出词条（卡级修饰型，标记）：不进效果管线，玩法层据它驳回打出命令。</summary>
    public sealed class CA_UnableToPlay : CardAffix
    {
        internal CA_UnableToPlay(AD_UnableToPlay unableToPlay_)
            : this(unableToPlay_.Name, unableToPlay_.ShowDescriptionCardView)
        {
        }

        private CA_UnableToPlay(string name_, bool showDescription_)
        {
            Name = name_;
            ShowDescription = showDescription_;
        }

        public override string Name { get; }
        public override bool ShowDescription { get; }
        internal override int PipelineOrder => -1;

        public override string GetDescription(IStatRenderer valueRenderer_ = null) => "这张牌无法被打出";

        #region 深拷贝一份独立词条
        public override CardAffix Clone() => new CA_UnableToPlay(Name, ShowDescription);
        #endregion
    }
}
