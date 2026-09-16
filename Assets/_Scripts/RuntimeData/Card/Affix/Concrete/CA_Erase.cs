using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>遗忘词条（效果型，标记）：打出时从整局游戏永久移除自身，区别于消耗仅本场离场。</summary>
    public class CA_Erase : CardAffix
    {
        internal CA_Erase(AD_Erase erase_) : this(erase_.Name, erase_.ShowDescriptionCardView)
        {
        }

        private CA_Erase(string name_, bool showDescription_)
        {
            Name = name_;
            ShowDescription = showDescription_;
        }

        public override string Name { get; }
        public override bool ShowDescription { get; }
        internal override int PipelineOrder => 6;

        public override string GetDescription() => "打出后从整局游戏中永久移除自身";

        #region 深拷贝一份独立词条
        public override CardAffix Clone() => new CA_Erase(Name, ShowDescription);
        #endregion
    }
}
