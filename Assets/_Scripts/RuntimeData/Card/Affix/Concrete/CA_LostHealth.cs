using _DreamKeep.ConfigData;
using _DreamKeep.Utility;

namespace _DreamKeep.RuntimeData
{
    /// <summary>失去生命词条（效果型）：目标失去指定数值的生命，无视格挡，也不参与力量/易伤等伤害修正。</summary>
    public sealed class CA_LostHealth : CardAffix
    {
        internal CA_LostHealth(AD_LostHealth lostHealth_) : this(lostHealth_.Name, lostHealth_.ShowDescriptionCardView,
            new ModifiableStat(lostHealth_.Amount))
        {
        }

        private CA_LostHealth(string name_, bool showDescription_, ModifiableStat amount_)
        {
            Name = name_;
            ShowDescription = showDescription_;
            Amount = amount_;
        }

        public override string Name { get; }
        public override bool ShowDescription { get; }
        public ModifiableStat Amount { get; }
        internal override int PipelineOrder => 0;

        // 失去生命不吃伤害修正，数值只随升级增量变化，故不走伤害通道求值
        public override string GetDescription(IStatRenderer valueRenderer_ = null) => $"失去 {Amount.Value} 点生命";

        #region 深拷贝一份独立词条，数值容器连同已生效的修正一并带走
        public override CardAffix Clone() => new CA_LostHealth(Name, ShowDescription, Amount.Clone());
        #endregion
    }
}
