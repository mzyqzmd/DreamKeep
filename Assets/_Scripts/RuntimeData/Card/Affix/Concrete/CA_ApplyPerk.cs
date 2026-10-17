using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>施加状态词条（效果型）：为指定目标施加一个状态原型，层数由原型携带。</summary>
    public sealed class CA_ApplyPerk : CardAffix
    {
        private readonly int _baseLayers; // 配置里的原始层数：升级只改状态原型的层数，比较基准只能记在词条上

        internal CA_ApplyPerk(AD_ApplyPerk applyPerk_, BattlePerk perk_)
            : this(applyPerk_.Name, applyPerk_.ShowDescriptionCardView, perk_, applyPerk_.Layers)
        {
        }

        private CA_ApplyPerk(string name_, bool showDescription_, BattlePerk perk_, int baseLayers_)
        {
            Name = name_;
            ShowDescription = showDescription_;
            Perk = perk_;
            _baseLayers = baseLayers_;
        }

        public override string Name { get; }
        public override bool ShowDescription { get; }
        public BattlePerk Perk { get; }
        internal override int PipelineOrder => 2;

        /// <summary>层数经渲染器着色（高于原始层数标升色），状态名不参与。</summary>
        public override string GetDescription(IStatRenderer valueRenderer_ = null)
        {
            string layers = valueRenderer_?.Render(_baseLayers, Perk.Layers, EStatChannel.PerkLayers) ?? Perk.Layers.ToString();
            return $"对目标施加 {layers} 层{Perk.Name}";
        }

        #region 深拷贝一份独立词条，状态原型连同层数一并克隆（基准层数保持原值）

        public override CardAffix Clone() => new CA_ApplyPerk(Name, ShowDescription, Perk.Clone(), _baseLayers);

        #endregion
    }
}