using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「恶魔形态」运行时实例：层数即宿主回合开始获得的力量，常驻不衰减。</summary>
    public sealed class BP_DemonForm : BattlePerk
    {
        private SO_DemonForm Config => (SO_DemonForm)_data;

        public BP_DemonForm(SO_DemonForm data_) : base(data_) { }

        public SO_Strength Strength => Config.Strength;

        public override bool Settle() => false;

        public override string GetDescription() => GetDescription(null, Layers);

        public override string GetDescription(IStatRenderer valueRenderer_, int baseLayers_) =>
            $"在你的回合开始时，\n获得 {RenderLayers(valueRenderer_, baseLayers_)} 点力量";

        public override BattlePerk Clone() => WithLayers(new BP_DemonForm(Config));
    }
}
