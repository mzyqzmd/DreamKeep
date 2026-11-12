using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「领地意识」运行时实例：层数即宿主回合末获得的力量点数，常驻不衰减、不移除。</summary>
    public sealed class BP_Territorial : BattlePerk
    {
        private SO_Territorial Config => (SO_Territorial)_data;

        public BP_Territorial(SO_Territorial data_) : base(data_) { }

        public SO_Strength Strength => Config.Strength;

        public override bool Settle() => false;

        public override string GetDescription() => $"自身回合结束时，获得 {Layers} 点力量";

        public override BattlePerk Clone() => WithLayers(new BP_Territorial(Config));
    }
}
