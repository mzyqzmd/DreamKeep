using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「缓冲」运行时实例：层数是将要挡下的次数，每次受伤改为 1 点并递减一层。</summary>
    public sealed class BP_Buffer : BattlePerk
    {
        private SO_Buffer Config => (SO_Buffer)_data;

        public BP_Buffer(SO_Buffer data_) : base(data_) { }

        public override bool Settle() => false;

        public override string GetDescription() => $"抵消接下来 {Layers} 次\n未被格挡的伤害";

        public override BattlePerk Clone() => WithLayers(new BP_Buffer(Config));
    }
}
