using _DreamKeep.ConfigData;
using _DreamKeep.Utility;

namespace _DreamKeep.RuntimeData
{
    /// <summary>伤害词条（效果型）：对目标造成指定数值的伤害，可多次结算。</summary>
    public sealed class CA_Damage : CardAffix
    {
        internal CA_Damage(AD_Damage damage_) : this(damage_.Name, damage_.ShowDescriptionCardView,
            new ModifiableStat(damage_.Amount), new ModifiableStat(damage_.Times))
        {
        }

        private CA_Damage(string name_, bool showDescription_, ModifiableStat amount_, ModifiableStat times_)
        {
            Name = name_;
            ShowDescription = showDescription_;
            Amount = amount_;
            Times = times_;
        }

        public override string Name { get; }
        public override bool ShowDescription { get; }
        public ModifiableStat Amount { get; }
        public ModifiableStat Times { get; }
        internal override int PipelineOrder => 0;

        /// <summary>伤害值与攻击次数经渲染器着色（修正求值与颜色归表现层）。</summary>
        public override string GetDescription(IStatRenderer valueRenderer_ = null)
        {
            string amount = valueRenderer_?.Render(Amount.BaseValue, Amount.Value, EStatChannel.Damage) ?? Amount.Value.ToString();
            string times = valueRenderer_?.Render(Times.BaseValue, Times.Value, EStatChannel.DamageTimes) ?? Times.Value.ToString();
            return Times.Value > 1 ? $"造成 {amount} 点伤害 {times} 次" : $"造成 {amount} 点伤害";
        }

        #region 深拷贝一份独立词条，数值容器连同已生效的修正一并带走

        public override CardAffix Clone() => new CA_Damage(Name, ShowDescription, Amount.Clone(), Times.Clone());

        #endregion
    }
}