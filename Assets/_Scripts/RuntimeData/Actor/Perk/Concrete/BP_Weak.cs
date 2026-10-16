using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「虚弱」运行时实例：降低造成的伤害，参数转发自配置模板。</summary>
    public sealed class BP_Weak : BattlePerk
    {
        private PD_Weak Config => (PD_Weak)_data;

        public BP_Weak(PD_Weak data_) : base(data_) { }

        public float Ratio => Config.Ratio;
        public override string GetDescription() => $"接下来 {Layers} 回合内，造成的伤害 - {Ratio * 100} %";
        public override BattlePerk Clone() => WithLayers(new BP_Weak(Config));
    }
}
