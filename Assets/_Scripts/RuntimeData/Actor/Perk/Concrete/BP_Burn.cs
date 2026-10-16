using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「灼烧」运行时实例：回合结束时受到等于层数的伤害，随后层数递减。</summary>
    public sealed class BP_Burn : BattlePerk
    {
        private PD_Burn Config => (PD_Burn)_data;

        public BP_Burn(PD_Burn data_) : base(data_) { }

        public override string GetDescription() => $"回合结束受到 {Layers} 点伤害，随后层数-1";
        public override BattlePerk Clone() => WithLayers(new BP_Burn(Config));
    }
}
