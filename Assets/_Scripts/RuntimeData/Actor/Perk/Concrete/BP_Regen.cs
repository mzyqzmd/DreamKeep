using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「再生」运行时实例：回合结束恢复等于层数的生命，随后层数递减。</summary>
    public sealed class BP_Regen : BattlePerk
    {
        private PD_Regen Config => (PD_Regen)_data;

        public BP_Regen(PD_Regen data_) : base(data_) { }

        public override string GetDescription() => $"回合结束恢复 {Layers} 点生命值，随后层数-1";
        public override BattlePerk Clone() => WithLayers(new BP_Regen(Config));
    }
}
