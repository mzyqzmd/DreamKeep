using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>消耗词条（效果型，标记）：打出后进入消耗区，本场战斗不再被抽到。</summary>
    public sealed class CA_Exhaust : CardAffix
    {
        internal CA_Exhaust(AD_Exhaust exhaust_) : this(exhaust_.Name, exhaust_.ShowDescriptionCardView)
        {
        }

        private CA_Exhaust(string name_, bool showDescription_)
        {
            Name = name_;
            ShowDescription = showDescription_;
        }

        public override string Name { get; }
        public override bool ShowDescription { get; }
        internal override int PipelineOrder => 7;

        public override string GetDescription() => "打出后消耗自身本场战斗不再被抽到";

        #region 深拷贝一份独立词条
        public override CardAffix Clone() => new CA_Exhaust(Name, ShowDescription);
        #endregion
    }
}
