using _DreamKeep.ConfigData;
using _DreamKeep.Utility;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「超巨化」运行时实例：倍率在烘焙时从定义资产拷进本实例，下一张攻击牌结算时由出牌入口消费。</summary>
    public sealed class BP_Gigantification : BattlePerk
    {
        private readonly float _multiplier;

        private SO_Gigantification Config => (SO_Gigantification)_data;

        public BP_Gigantification(SO_Gigantification data_) : base(data_) => _multiplier = data_.Multiplier;

        private BP_Gigantification(SO_Gigantification data_, float multiplier_) : base(data_) => _multiplier = multiplier_;

        public float Multiplier => _multiplier;

        /// <summary>乘性贡献：宿主自己出手且未消费时提高伤害；预览与结算共用同一份规则。</summary>
        public StatModifier GetModifier(EStatChannel channel_, Creature host_, Creature source_, Creature target_) =>
            channel_ == EStatChannel.Damage && host_ == source_ ? new StatModifier(0, _multiplier) : StatModifier.Neutral;

        public override bool Settle() => false;

        public override string GetDescription() => $"下一张攻击牌造成的伤害\n变为 {_multiplier:0.#} 倍";

        public override BattlePerk Clone() => WithLayers(new BP_Gigantification(Config, _multiplier));
    }
}
