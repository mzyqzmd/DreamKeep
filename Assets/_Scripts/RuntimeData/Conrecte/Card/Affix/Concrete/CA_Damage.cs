using _DreamKeep.Data;
using _DreamKeep.Utility;

namespace _DreamKeep.RuntimeData
{
    /// <summary>伤害词条（效果型）：对目标造成指定数值的伤害，可多次结算。</summary>
    public class CA_Damage : CardAffix
    {
        internal CA_Damage(AD_Damage damage_) : this(damage_.Name, damage_.ShowDescriptionCardView, damage_.Amount, damage_.Times)
        {
        }

        private CA_Damage(string name_, bool showDescription_, int amount_, int times_)
        {
            Name = name_;
            ShowDescription = showDescription_;
            Amount = new ModifiableStat(amount_);
            Times = new ModifiableStat(times_);
        }

        public override string Name { get; }
        public override bool ShowDescription { get; }
        public ModifiableStat Amount { get; }
        public ModifiableStat Times { get; }
        internal override int PipelineOrder => 0;

        public override string GetDescription() => Times.Value > 1 ? $"造成 {Amount.Value} 点\n伤害 {Times.Value} 次" : $"造成 {Amount.Value} 点伤害";

        #region 深拷贝一份独立词条
        public override CardAffix Clone() => new CA_Damage(Name, ShowDescription, Amount.Value, Times.Value);
        #endregion
    }
}
