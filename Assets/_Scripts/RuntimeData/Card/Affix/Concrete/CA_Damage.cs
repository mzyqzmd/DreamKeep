using System;
using System.Text;
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

        /// <summary>伤害值经渲染器着色（修正求值与颜色归表现层），次数不参与。</summary>
        public override string GetDescription(IValueRenderer valueRenderer_ = null)
        {
            if (valueRenderer_ == null) throw new NullReferenceException("此处需要文本renderer但是此处为空!");
            StringBuilder sb = new($"造成 {valueRenderer_.Render(Amount.Value)} 点伤害");
            if (Times.Value > 1) sb.Append($" {Times.Value} 次");
            return sb.ToString();
        }

        #region 深拷贝一份独立词条，数值容器连同已生效的修正一并带走

        public override CardAffix Clone() => new CA_Damage(Name, ShowDescription, Amount.Clone(), Times.Clone());

        #endregion
    }
}