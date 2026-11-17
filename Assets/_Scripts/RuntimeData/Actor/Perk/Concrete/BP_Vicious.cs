using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「凶恶」运行时实例：层数即每次给予易伤触发的抽牌张数，战斗内不衰减。</summary>
    public sealed class BP_Vicious : BattlePerk
    {
        private SO_Vicious Config => (SO_Vicious)_data;

        public BP_Vicious(SO_Vicious data_) : base(data_) { }

        public override string GetDescription() => GetDescription(null, Layers);

        public override string GetDescription(IStatRenderer valueRenderer_, int baseLayers_) =>
            $"每当你给予易伤时，\n抽 {RenderLayers(valueRenderer_, baseLayers_)} 张牌";

        public override BattlePerk Clone() => WithLayers(new BP_Vicious(Config));
    }
}
