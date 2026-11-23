using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「仪式」运行时实例：层数是每回合开始获得的力量，常驻不衰减。</summary>
    public sealed class BP_Ritual : BattlePerk
    {
        private SO_Ritual Config => (SO_Ritual)_data;

        public BP_Ritual(SO_Ritual data_) : base(data_) { }

        public SO_Strength Strength => Config.Strength;

        public override bool Settle() => false;

        public override string GetDescription() => $"回合开始时获得 {Layers} 点力量";

        public override BattlePerk Clone() => WithLayers(new BP_Ritual(Config));
    }
}
