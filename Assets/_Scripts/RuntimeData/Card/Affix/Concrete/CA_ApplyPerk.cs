using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>施加状态词条（效果型）：为指定目标施加一个状态原型，层数由原型携带。</summary>
    public sealed class CA_ApplyPerk : CardAffix
    {
        internal CA_ApplyPerk(AD_ApplyPerk applyPerk_, BattlePerk perk_)
            : this(applyPerk_.Name, applyPerk_.ShowDescriptionCardView, perk_)
        {
        }

        private CA_ApplyPerk(string name_, bool showDescription_, BattlePerk perk_)
        {
            Name = name_;
            ShowDescription = showDescription_;
            Perk = perk_;
        }

        public override string Name { get; }
        public override bool ShowDescription { get; }
        public BattlePerk Perk { get; }
        internal override int PipelineOrder => 2;

        public override string GetDescription(IStatRenderer valueRenderer_ = null) => $"对目标施加 {Perk.Layers} 层{Perk.Name}";

        #region 深拷贝一份独立词条，状态原型连同层数一并克隆

        public override CardAffix Clone() => new CA_ApplyPerk(Name, ShowDescription, Perk.Clone());

        #endregion
    }
}