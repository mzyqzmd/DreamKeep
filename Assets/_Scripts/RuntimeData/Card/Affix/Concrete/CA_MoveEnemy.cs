using _DreamKeep.ConfigData;
using _DreamKeep.Utility;

namespace _DreamKeep.RuntimeData
{
    /// <summary>位移词条（效果型）：把目标在敌人槽位间推动若干格，正数推远、负数拉近。
    /// 位移量不参与数值着色（它不是伤害也不是格挡），描述按方向直接出文案。</summary>
    public sealed class CA_MoveEnemy : CardAffix
    {
        internal CA_MoveEnemy(AD_MoveEnemy move_) : this(move_.Name, move_.ShowDescriptionCardView,
            new ModifiableStat(move_.Offset))
        {
        }

        private CA_MoveEnemy(string name_, bool showDescription_, ModifiableStat offset_)
        {
            Name = name_;
            ShowDescription = showDescription_;
            Offset = offset_;
        }

        public override string Name { get; }
        public override bool ShowDescription { get; }
        public ModifiableStat Offset { get; }
        internal override int PipelineOrder => 2;

        public override string GetDescription(IStatRenderer valueRenderer_ = null)
        {
            int offset = Offset.Value;
            if (offset > 0) return $"将目标击退 {offset} 格";
            if (offset < 0) return $"将目标拉近 {offset} 格";
            return "不改变目标所在格";
        }

        #region 深拷贝一份独立词条，数值容器连同已生效的修正一并带走
        public override CardAffix Clone() => new CA_MoveEnemy(Name, ShowDescription, Offset.Clone());
        #endregion
    }
}
