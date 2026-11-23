using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「覆甲」运行时实例：层数是每回合结束获得的格挡值，随后递减一层。</summary>
    public sealed class BP_Plating : BattlePerk
    {
        private SO_Plating Config => (SO_Plating)_data;

        public BP_Plating(SO_Plating data_) : base(data_) { }

        public override string GetDescription() => $"回合结束时获得 {Layers} 点格挡，\n随后层数 - 1";

        public override BattlePerk Clone() => WithLayers(new BP_Plating(Config));
    }
}
