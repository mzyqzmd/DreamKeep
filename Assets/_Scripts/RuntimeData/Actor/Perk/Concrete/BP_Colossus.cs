using _DreamKeep.ConfigData;
using _DreamKeep.Utility;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「巨像」运行时实例：攻击者带易伤时，宿主受到的伤害按比率降低，回合结束自动移除。</summary>
    public sealed class BP_Colossus : BattlePerk
    {
        private readonly float _damageReduction;

        private SO_Colossus Config => (SO_Colossus)_data;

        public BP_Colossus(SO_Colossus data_) : base(data_) => _damageReduction = data_.DamageReduction;

        private BP_Colossus(SO_Colossus data_, float reduction_) : base(data_) => _damageReduction = reduction_;

        /// <summary>乘性贡献：宿主自己挨打且攻击方带易伤时降低为 ×(1-reduction)。</summary>
        public StatModifier GetModifier(EStatChannel channel_, Creature host_, Creature source_, Creature target_) =>
            channel_ == EStatChannel.Damage && host_ == target_ && BattlePerkQuery.Has<BP_Vulnerable>(source_)
                ? new StatModifier(0, 1f - _damageReduction)
                : StatModifier.Neutral;

        public override string GetDescription() =>
            $"本回合内，有易伤状态的\n敌人对你造成的\n伤害降低 {_damageReduction * 100} %";

        public override BattlePerk Clone() => WithLayers(new BP_Colossus(Config, _damageReduction));
    }
}
