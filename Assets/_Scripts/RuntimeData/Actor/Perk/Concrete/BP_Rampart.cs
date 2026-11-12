using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「高墙」运行时实例：层数即每次结算发放的格挡值，宿主回合结束触发后常驻，不递减也不移除。</summary>
    public sealed class BP_Rampart : BattlePerk
    {
        private SO_Rampart Config => (SO_Rampart)_data;

        public BP_Rampart(SO_Rampart data_) : base(data_) { }

        public override bool Settle() => false;

        public override string GetDescription() => $"回合结束时，自己和盟友\n获得 {Layers} 点格挡";

        public override BattlePerk Clone() => WithLayers(new BP_Rampart(Config));
    }
}
