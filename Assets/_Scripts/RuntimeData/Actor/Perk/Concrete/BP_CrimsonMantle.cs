using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「绯红披风」运行时实例：宿主回合开始先失去固定生命再获得等同层数的格挡，常驻不衰减。</summary>
    public sealed class BP_CrimsonMantle : BattlePerk
    {
        private SO_CrimsonMantle Config => (SO_CrimsonMantle)_data;

        public BP_CrimsonMantle(SO_CrimsonMantle data_) : base(data_) { }

        public int HpCost => Config.HpCost;

        public override bool Settle() => false;

        public override string GetDescription() => GetDescription(null, Layers);

        public override string GetDescription(IStatRenderer valueRenderer_, int baseLayers_) =>
            $"回合开始时失去 {HpCost} 点生命，\n获得 {RenderLayers(valueRenderer_, baseLayers_)} 点格挡";

        public override BattlePerk Clone() => WithLayers(new BP_CrimsonMantle(Config));
    }
}
