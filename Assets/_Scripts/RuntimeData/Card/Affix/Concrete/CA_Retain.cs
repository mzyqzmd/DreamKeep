using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>保留词条（卡级修饰型，标记）：回合结束不进弃牌堆，仅当被打出时才离场。</summary>
    public class CA_Retain : CardAffix
    {
        internal CA_Retain(AD_Retain retain_) : this(retain_.Name, retain_.ShowDescriptionCardView)
        {
        }

        private CA_Retain(string name_, bool showDescription_)
        {
            Name = name_;
            ShowDescription = showDescription_;
        }

        public override string Name { get; }
        public override bool ShowDescription { get; }
        internal override int PipelineOrder => -1;

        public override string GetDescription() => "回合结束时拥有此词条的卡牌不会进入弃牌堆";

        #region 深拷贝一份独立词条
        public override CardAffix Clone() => new CA_Retain(Name, ShowDescription);
        #endregion
    }
}
