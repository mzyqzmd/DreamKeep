using _DreamKeep.ConfigData;
using _DreamKeep.Utility;

namespace _DreamKeep.RuntimeData
{
    /// <summary>治疗词条（效果型）：按卡牌落点为指定目标恢复指定数值的生命，满血时钳到上限，不参与力量/易伤等伤害修正。</summary>
    public sealed class CA_Heal : CardAffix
    {
        internal CA_Heal(AD_Heal heal_) : this(heal_.Name, heal_.ShowDescriptionCardView,
            new ModifiableStat(heal_.Amount))
        {
        }

        private CA_Heal(string name_, bool showDescription_, ModifiableStat amount_)
        {
            Name = name_;
            ShowDescription = showDescription_;
            Amount = amount_;
        }

        public override string Name { get; }
        public override bool ShowDescription { get; }
        public ModifiableStat Amount { get; }
        internal override int PipelineOrder => 0;

        // 治疗不吃伤害修正，静态与升级前基准比较着色（治疗更多标升色、低于不标色）
        public override string GetDescription(IStatRenderer valueRenderer_ = null) =>
            $"恢复目标 {valueRenderer_?.Render(Amount.BaseValue, Amount.Value, EStatChannel.Heal) ?? Amount.Value.ToString()} 点生命";

        #region 深拷贝一份独立词条，数值容器连同已生效的修正一并带走
        public override CardAffix Clone() => new CA_Heal(Name, ShowDescription, Amount.Clone());
        #endregion
    }
}
