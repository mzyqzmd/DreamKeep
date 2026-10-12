using System.Text;
using _DreamKeep.ConfigData;
using _DreamKeep.Utility;

namespace _DreamKeep.RuntimeData
{
    /// <summary>获得能量词条（效果型）：为自身获得指定数值的能量。</summary>
    public sealed class CA_GainEnergy : CardAffix
    {
        internal CA_GainEnergy(AD_GainEnergy gainEnergy_) : this(gainEnergy_.Name, gainEnergy_.ShowDescriptionCardView,
            gainEnergy_.Amount)
        {
        }

        private CA_GainEnergy(string name_, bool showDescription_, int amount_)
        {
            Name = name_;
            ShowDescription = showDescription_;
            Amount = new ModifiableStat(amount_);
        }

        public override string Name { get; }
        public override bool ShowDescription { get; }
        public ModifiableStat Amount { get; }
        internal override int PipelineOrder => 4;

        public override string GetDescription()
        {
            StringBuilder sb = new();
            sb.Append("获得 ");
            for (int i = 0; i < Amount.Value; i++)
                sb.Append("<sprite name=\"energyIcon\">");

            sb.Append(" 。");
            return sb.ToString();
        }

        #region 深拷贝一份独立词条

        public override CardAffix Clone() => new CA_GainEnergy(Name, ShowDescription, Amount.Value);

        #endregion
    }
}