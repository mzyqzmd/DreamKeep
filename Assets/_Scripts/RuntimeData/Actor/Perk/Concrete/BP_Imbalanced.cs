using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「失衡」运行时实例：常驻不衰减、不移除，触发结算住在 GameBattle 的 FullBlockHook。</summary>
    public sealed class BP_Imbalanced : BattlePerk
    {
        public BP_Imbalanced(SO_Imbalanced data_) : base(data_) { }

        public override bool Settle() => false;

        public override string GetDescription() => "如果这个生物的攻击被\n完全格挡，则它将被击晕";

        public override BattlePerk Clone() => WithLayers(new BP_Imbalanced((SO_Imbalanced)_data));
    }
}