using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「易伤」运行时实例：提高受到的伤害，参数转发自配置模板。</summary>
    public sealed class BP_Vulnerable : BattlePerk
    {
        private PD_Vulnerable Config => (PD_Vulnerable)_data;

        public BP_Vulnerable(PD_Vulnerable data_) : base(data_) { }

        public float Ratio => Config.Ratio;
        public override string GetDescription() => $"接下来 {Layers} 回合内，受到的伤害+ {Ratio * 100} %";
        public override BattlePerk Clone() => WithLayers(new BP_Vulnerable(Config));
    }
}
