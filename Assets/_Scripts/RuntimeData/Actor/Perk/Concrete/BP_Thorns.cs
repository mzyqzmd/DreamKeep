using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「荆棘」运行时实例：层数是受击反伤值，常驻不衰减。</summary>
    public sealed class BP_Thorns : BattlePerk
    {
        private SO_Thorns Config => (SO_Thorns)_data;

        public BP_Thorns(SO_Thorns data_) : base(data_) { }

        public override bool Settle() => false;

        public override string GetDescription() => $"受到攻击时，对攻击者造成 {Layers} 点伤害";

        public override BattlePerk Clone() => WithLayers(new BP_Thorns(Config));
    }
}
