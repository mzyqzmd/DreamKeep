using _DreamKeep.ConfigData;
using _DreamKeep.Utility;

namespace _DreamKeep.RuntimeData
{
    /// <summary>选择升级词条（运行时）：本场战斗中升级手牌，档位为全部或指定张数。</summary>
    public sealed class CA_SelectUpgrade : CardAffix
    {
        internal CA_SelectUpgrade(AD_SelectUpgrade select_) : this(select_.Name, select_.ShowDescriptionCardView,
            select_.Scope, new ModifiableStat(select_.Count))
        {
        }

        private CA_SelectUpgrade(string name_, bool showDescription_, ESelectScope scope_, ModifiableStat count_)
        {
            Name = name_;
            ShowDescription = showDescription_;
            Scope = scope_;
            Count = count_;
        }

        public override string Name { get; }
        public override bool ShowDescription { get; }
        public ESelectScope Scope { get; }
        public ModifiableStat Count { get; }
        internal override int PipelineOrder => 0;

        public override string GetDescription(IStatRenderer valueRenderer_ = null) => Scope switch
        {
            ESelectScope.AllHand => "升级手牌中的所有牌",
            ESelectScope.Exact => $"从你的手牌中\n选取 {RenderCount(valueRenderer_)} 张进行升级",
            ESelectScope.UpTo => $"从你的手牌中至多\n选取 {RenderCount(valueRenderer_)} 张进行升级",
            _ => "你能看到这句话说明游戏有bug",
        };

        private string RenderCount(IStatRenderer valueRenderer_) =>
            valueRenderer_?.Render(Count.BaseValue, Count.Value, EStatChannel.SelectCount) ?? Count.Value.ToString();

        #region 深拷贝一份独立词条，数值容器连同已生效的修正一并带走

        public override CardAffix Clone() => new CA_SelectUpgrade(Name, ShowDescription, Scope, Count.Clone());

        #endregion
    }
}