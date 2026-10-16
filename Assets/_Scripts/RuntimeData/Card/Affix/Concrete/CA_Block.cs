using _DreamKeep.ConfigData;
using _DreamKeep.Utility;

namespace _DreamKeep.RuntimeData
{
    /// <summary>格挡词条（效果型）：为自身获得指定数值的格挡。</summary>
    public sealed class CA_Block : CardAffix
    {
        internal CA_Block(AD_Block block_) : this(block_.Name, block_.ShowDescriptionCardView, block_.Amount)
        {
        }

        private CA_Block(string name_, bool showDescription_, int amount_)
        {
            Name = name_;
            ShowDescription = showDescription_;
            Amount = new ModifiableStat(amount_);
        }

        public override string Name { get; }
        public override bool ShowDescription { get; }
        public ModifiableStat Amount { get; }
        internal override int PipelineOrder => 1;

        // 数值属格挡通道、修正未接入，暂原值输出
        public override string GetDescription(IValueRenderer valueRenderer_ = null) => $"获得 {Amount.Value} 点格挡";

        #region 深拷贝一份独立词条
        public override CardAffix Clone() => new CA_Block(Name, ShowDescription, Amount.Value);
        #endregion
    }
}
