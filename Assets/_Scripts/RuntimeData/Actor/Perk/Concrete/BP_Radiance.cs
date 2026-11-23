using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「光辉」运行时实例：层数是剩余回合数，每回合开始时额外获得 1 能量并递减一层。</summary>
    public sealed class BP_Radiance : BattlePerk
    {
        private SO_Radiance Config => (SO_Radiance)_data;

        public BP_Radiance(SO_Radiance data_) : base(data_) { }

        public override string GetDescription() => $"接下来 {Layers} 个回合开始时\n额外获得 1 点能量";

        public override BattlePerk Clone() => WithLayers(new BP_Radiance(Config));
    }
}
