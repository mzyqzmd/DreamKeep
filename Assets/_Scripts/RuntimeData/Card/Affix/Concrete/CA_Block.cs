using _DreamKeep.ConfigData;
using _DreamKeep.Utility;

namespace _DreamKeep.RuntimeData
{
    /// <summary>格挡词条（效果型）：为自身获得指定数值的格挡。</summary>
    public sealed class CA_Block : CardAffix
    {
        internal CA_Block(AD_Block block_) : this(block_.Name, block_.ShowDescriptionCardView,
            new ModifiableStat(block_.Amount))
        {
        }

        private CA_Block(string name_, bool showDescription_, ModifiableStat amount_)
        {
            Name = name_;
            ShowDescription = showDescription_;
            Amount = amount_;
        }

        public override string Name { get; }
        public override bool ShowDescription { get; }
        public ModifiableStat Amount { get; }
        internal override int PipelineOrder => 1;

        // 数值属格挡通道：升级增量走容器已生效，战斗内的格挡修正通道尚未接入
        public override string GetDescription(IValueRenderer valueRenderer_ = null) => $"获得 {Amount.Value} 点格挡";

        #region 深拷贝一份独立词条，数值容器连同已生效的修正一并带走
        public override CardAffix Clone() => new CA_Block(Name, ShowDescription, Amount.Clone());
        #endregion
    }
}
