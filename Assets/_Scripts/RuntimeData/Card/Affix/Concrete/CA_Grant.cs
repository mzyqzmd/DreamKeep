using _DreamKeep.ConfigData;
using _DreamKeep.Utility;

namespace _DreamKeep.RuntimeData
{
    /// <summary>给予词条（效果型）：打出后为指定张数的牌追加一个词条，运行时动态增删。</summary>
    public sealed class CA_Grant : CardAffix
    {
        internal CA_Grant(AD_Grant grant_, CardAffix grantedAffix_)
            : this(grant_.Name, grant_.ShowDescriptionCardView, new ModifiableStat(grant_.Count), grantedAffix_)
        {
        }

        private CA_Grant(string name_, bool showDescription_, ModifiableStat count_, CardAffix grantedAffix_)
        {
            Name = name_;
            ShowDescription = showDescription_;
            Count = count_;
            GrantedAffix = grantedAffix_;
        }

        public override string Name { get; }
        public override bool ShowDescription { get; }
        public ModifiableStat Count { get; }
        public CardAffix GrantedAffix { get; }
        internal override int PipelineOrder => 5;

        // 数量属追加词条通道：升级增量走容器已生效，战斗内的数量修正通道尚未接入
        public override string GetDescription(IStatRenderer valueRenderer_ = null) => $"打出后为 {Count.Value} 张牌追加 {GrantedAffix.Name} 词条";

        #region 深拷贝一份独立词条，被授予的词条递归克隆，数值容器连同已生效的修正一并带走
        public override CardAffix Clone() => new CA_Grant(Name, ShowDescription, Count.Clone(), GrantedAffix.Clone());
        #endregion
    }
}
