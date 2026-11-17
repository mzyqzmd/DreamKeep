using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「狱火」运行时实例：宿主回合开始失去固定生命，回合内每次失去生命对所有敌人造成等同层数的伤害，常驻不衰减。</summary>
    public sealed class BP_Inferno : BattlePerk
    {
        private SO_Inferno Config => (SO_Inferno)_data;

        public BP_Inferno(SO_Inferno data_) : base(data_) { }

        public int HpCost => Config.HpCost;

        public override bool Settle() => false;

        public override string GetDescription() => GetDescription(null, Layers);

        public override string GetDescription(IStatRenderer valueRenderer_, int baseLayers_) =>
            $"在你的回合开始时，\n失去 {HpCost} 点生命。每当你在\n回合内失去生命时，对\n所有敌人造成 {RenderLayers(valueRenderer_, baseLayers_)} 点伤害";

        public override BattlePerk Clone() => WithLayers(new BP_Inferno(Config));
    }
}
