using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「下回合格挡」运行时实例：层数是下回合开始获得的格挡值，触发后整条移除。</summary>
    public sealed class BP_BlockNextTurn : BattlePerk
    {
        private SO_BlockNextTurn Config => (SO_BlockNextTurn)_data;

        public BP_BlockNextTurn(SO_BlockNextTurn data_) : base(data_) { }

        public override bool Settle() => true;

        public override string GetDescription() => $"下个回合开始时\n获得 {Layers} 点格挡";

        public override BattlePerk Clone() => WithLayers(new BP_BlockNextTurn(Config));
    }
}
