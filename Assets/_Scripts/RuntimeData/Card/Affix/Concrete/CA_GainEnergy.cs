using System.Text;
using _DreamKeep.ConfigData;
using _DreamKeep.Utility;

namespace _DreamKeep.RuntimeData
{
    /// <summary>获得能量词条（效果型）：为出牌者（玩家）获得指定数值的能量，与卡牌以谁为目标无关。</summary>
    public sealed class CA_GainEnergy : CardAffix
    {
        internal CA_GainEnergy(AD_GainEnergy gainEnergy_) : this(gainEnergy_.Name, gainEnergy_.ShowDescriptionCardView,
            new ModifiableStat(gainEnergy_.Amount))
        {
        }

        private CA_GainEnergy(string name_, bool showDescription_, ModifiableStat amount_)
        {
            Name = name_;
            ShowDescription = showDescription_;
            Amount = amount_;
        }

        public override string Name { get; }
        public override bool ShowDescription { get; }
        public ModifiableStat Amount { get; }
        internal override int PipelineOrder => 0;

        // 数值属能量通道：升级增量走容器已生效，战斗内的能量修正通道尚未接入
        public override string GetDescription(IStatRenderer valueRenderer_ = null)
        {
            StringBuilder sb = new();
            sb.Append("玩家获得 ");
            for (int i = 0; i < Amount.Value; i++)
                sb.Append("<sprite name=\"energyIcon\">");

            sb.Append(" 。");
            return sb.ToString();
        }

        #region 深拷贝一份独立词条，数值容器连同已生效的修正一并带走

        public override CardAffix Clone() => new CA_GainEnergy(Name, ShowDescription, Amount.Clone());

        #endregion
    }
}