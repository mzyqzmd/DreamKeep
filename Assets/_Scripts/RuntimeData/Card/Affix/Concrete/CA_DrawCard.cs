using _DreamKeep.ConfigData;
using _DreamKeep.Utility;

namespace _DreamKeep.RuntimeData
{
    /// <summary>抽牌词条（效果型）：从抽牌堆抽指定张数的牌。</summary>
    public class CA_DrawCard : CardAffix
    {
        internal CA_DrawCard(AD_DrawCard drawCard_) : this(drawCard_.Name, drawCard_.ShowDescriptionCardView, drawCard_.Count)
        {
        }

        private CA_DrawCard(string name_, bool showDescription_, int count_)
        {
            Name = name_;
            ShowDescription = showDescription_;
            Count = new ModifiableStat(count_);
        }

        public override string Name { get; }
        public override bool ShowDescription { get; }
        public ModifiableStat Count { get; }
        internal override int PipelineOrder => 3;

        public override string GetDescription() => $"抽 {Count.Value} 张牌";

        #region 深拷贝一份独立词条
        public override CardAffix Clone() => new CA_DrawCard(Name, ShowDescription, Count.Value);
        #endregion
    }
}
