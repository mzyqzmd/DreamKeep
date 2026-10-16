using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「易伤」运行时实例：提高受到的伤害（供伤害管线的乘性阶段读取）。
    /// 比率在烘焙时从定义资产拷进本实例，之后流程若要改值改的是实例，定义资产永不被写。</summary>
    public sealed class BP_Vulnerable : BattlePerk
    {
        private readonly float _ratio;

        private SO_Vulnerable Config => (SO_Vulnerable)_data;

        public BP_Vulnerable(SO_Vulnerable data_) : base(data_) => _ratio = data_.Ratio;

        private BP_Vulnerable(SO_Vulnerable data_, float ratio_) : base(data_) => _ratio = ratio_;

        public float Ratio => _ratio;

        /// <summary>乘性贡献：宿主自己挨打时提高为 ×(1+ratio)。</summary>
        public override float ModifyDamageMultiplicative(Creature host_, Creature source_, Creature target_) =>
            host_ == target_ ? 1f + _ratio : 1f;

        public override string GetDescription() => $"接下来 {Layers} 回合内，\n受到的伤害+ {_ratio * 100} %";
        public override BattlePerk Clone() => WithLayers(new BP_Vulnerable(Config, _ratio));
    }
}
