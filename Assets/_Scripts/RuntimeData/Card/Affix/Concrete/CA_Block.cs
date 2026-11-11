using _DreamKeep.ConfigData;
using _DreamKeep.Utility;

namespace _DreamKeep.RuntimeData
{
    /// <summary>格挡词条（效果型）：为出牌者（玩家）获得指定数值的格挡，与卡牌以谁为目标无关。</summary>
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
        internal override int PipelineOrder => 0;

        /// <summary>格挡值经渲染器着色（修正求值与颜色归表现层），升级增量已由数值容器承载。</summary>
        public override string GetDescription(IStatRenderer valueRenderer_ = null)
        {
            string amount = valueRenderer_?.Render(Amount.BaseValue, Amount.Value, EStatChannel.Block) ?? Amount.Value.ToString();
            return $"玩家获得 {amount} 点格挡";
        }

        #region 深拷贝一份独立词条，数值容器连同已生效的修正一并带走
        public override CardAffix Clone() => new CA_Block(Name, ShowDescription, Amount.Clone());
        #endregion
    }
}
