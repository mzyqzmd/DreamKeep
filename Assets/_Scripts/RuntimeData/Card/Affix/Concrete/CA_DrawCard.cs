using _DreamKeep.ConfigData;
using _DreamKeep.Utility;

namespace _DreamKeep.RuntimeData
{
    /// <summary>抽牌词条（效果型）：让出牌者（玩家）抽指定张数的牌，与卡牌以谁为目标无关。</summary>
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
        internal override int PipelineOrder => 0;

        // 抽牌数无战斗内修正，静态与升级前基准比较着色（高于基准标升色）
        public override string GetDescription(IStatRenderer valueRenderer_ = null) =>
            $"玩家抽 {valueRenderer_?.Render(Count.BaseValue, Count.Value, EStatChannel.DrawCard) ?? Count.Value.ToString()} 张牌";

        #region 深拷贝一份独立词条，数值容器连同已生效的修正一并带走
        public override CardAffix Clone() => new CA_DrawCard(Name, ShowDescription, Count.Clone());
        #endregion
    }
}
