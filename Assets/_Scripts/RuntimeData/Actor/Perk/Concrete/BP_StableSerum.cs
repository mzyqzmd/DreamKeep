using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「稳定血清」运行时实例：层数是剩余回合数，回合末保留整手并递减一层。</summary>
    public sealed class BP_StableSerum : BattlePerk
    {
        private SO_StableSerum Config => (SO_StableSerum)_data;

        public BP_StableSerum(SO_StableSerum data_) : base(data_) { }

        public override string GetDescription() => $"接下来 {Layers} 个回合\n回合结束时保留手牌";

        public override BattlePerk Clone() => WithLayers(new BP_StableSerum(Config));
    }
}
