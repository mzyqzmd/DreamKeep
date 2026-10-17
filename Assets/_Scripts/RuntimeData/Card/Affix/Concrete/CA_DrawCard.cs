using _DreamKeep.ConfigData;
using _DreamKeep.Utility;

namespace _DreamKeep.RuntimeData
{
    /// <summary>抽牌词条（效果型）：从抽牌堆抽指定张数的牌。</summary>
    public sealed class CA_DrawCard : CardAffix
    {
        internal CA_DrawCard(AD_DrawCard drawCard_) : this(drawCard_.Name, drawCard_.ShowDescriptionCardView,
            new ModifiableStat(drawCard_.Count))
        {
        }

        private CA_DrawCard(string name_, bool showDescription_, ModifiableStat count_)
        {
            Name = name_;
            ShowDescription = showDescription_;
            Count = count_;
        }

        public override string Name { get; }
        public override bool ShowDescription { get; }
        public ModifiableStat Count { get; }
        internal override int PipelineOrder => 3;

        // 数值属抽牌数通道：升级增量走容器已生效，战斗内的抽牌修正通道尚未接入
        public override string GetDescription(IStatRenderer valueRenderer_ = null) => $"抽 {Count.Value} 张牌";

        #region 深拷贝一份独立词条，数值容器连同已生效的修正一并带走
        public override CardAffix Clone() => new CA_DrawCard(Name, ShowDescription, Count.Clone());
        #endregion
    }
}
