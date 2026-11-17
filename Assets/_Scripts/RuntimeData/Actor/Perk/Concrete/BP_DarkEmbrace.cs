using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「黑暗之拥」运行时实例：层数即每次消耗触发的抽牌张数，战斗内不衰减。</summary>
    public sealed class BP_DarkEmbrace : BattlePerk
    {
        private SO_DarkEmbrace Config => (SO_DarkEmbrace)_data;

        public BP_DarkEmbrace(SO_DarkEmbrace data_) : base(data_) { }

        public override string GetDescription() => GetDescription(null, Layers);

        public override string GetDescription(IStatRenderer valueRenderer_, int baseLayers_) =>
            $"每当有一张牌被消耗时，\n抽 {RenderLayers(valueRenderer_, baseLayers_)} 张牌";

        public override BattlePerk Clone() => WithLayers(new BP_DarkEmbrace(Config));
    }
}
