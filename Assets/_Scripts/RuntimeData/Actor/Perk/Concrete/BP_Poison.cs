using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「中毒」运行时实例：回合结束时受到等于层数的伤害，随后层数递减。</summary>
    public sealed class BP_Poison : BattlePerk
    {
        private SO_Poison Config => (SO_Poison)_data;

        public BP_Poison(SO_Poison data_) : base(data_) { }

        public override string GetDescription() => $"回合结束受到 {Layers} 点\n伤害，随后层数-1";
        public override BattlePerk Clone() => WithLayers(new BP_Poison(Config));
    }
}
