using _DreamKeep.ConfigData;
using _DreamKeep.Utility;

namespace _DreamKeep.RuntimeData
{
    /// <summary>给予词条（效果型）：打出后为指定张数的牌追加一个词条，运行时动态增删。</summary>
    public class CA_Grant : CardAffix
    {
        internal CA_Grant(AD_Grant grant_, CardAffix grantedAffix_)
            : this(grant_.Name, grant_.ShowDescriptionCardView, grant_.Count, grantedAffix_)
        {
        }

        private CA_Grant(string name_, bool showDescription_, int count_, CardAffix grantedAffix_)
        {
            Name = name_;
            ShowDescription = showDescription_;
            Count = new ModifiableStat(count_);
            GrantedAffix = grantedAffix_;
        }

        public override string Name { get; }
        public override bool ShowDescription { get; }
        public ModifiableStat Count { get; }
        public CardAffix GrantedAffix { get; }
        internal override int PipelineOrder => 5;

        public override string GetDescription() => $"打出后为 {Count.Value} 张牌\n追加 {GrantedAffix.Name} 词条";

        #region 深拷贝一份独立词条，被授予的词条递归克隆，避免多张卡共享同一实例
        public override CardAffix Clone() => new CA_Grant(Name, ShowDescription, Count.Value, GrantedAffix.Clone());
        #endregion
    }
}
