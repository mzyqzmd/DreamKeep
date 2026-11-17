using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「无惧疼痛」运行时实例：层数即每次消耗触发的格挡值，战斗内不衰减。</summary>
    public sealed class BP_FeelNoPain : BattlePerk
    {
        private SO_FeelNoPain Config => (SO_FeelNoPain)_data;

        public BP_FeelNoPain(SO_FeelNoPain data_) : base(data_) { }

        public override string GetDescription() => GetDescription(null, Layers);

        public override string GetDescription(IStatRenderer valueRenderer_, int baseLayers_) =>
            $"每当有一张牌被消耗时，\n获得 {RenderLayers(valueRenderer_, baseLayers_)} 点格挡";

        public override BattlePerk Clone() => WithLayers(new BP_FeelNoPain(Config));
    }
}
