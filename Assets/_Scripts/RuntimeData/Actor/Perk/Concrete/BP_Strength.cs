using _DreamKeep.ConfigData;
using _DreamKeep.Utility;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「力量」运行时实例：每层提高 1 点攻击伤害（供 StatHook 分派表读取），战斗内不自动衰减。</summary>
    public sealed class BP_Strength : BattlePerk
    {
        private SO_Strength Config => (SO_Strength)_data;

        public BP_Strength(SO_Strength data_) : base(data_) { }

        /// <summary>加性贡献：宿主自己出手时每层 +1。</summary>
        public StatModifier GetModifier(EStatChannel channel_, Creature host_, Creature source_, Creature target_) =>
            channel_ == EStatChannel.Damage && host_ == source_ ? new StatModifier(Layers) : StatModifier.Neutral;

        public override string GetDescription() => $"从卡牌中造成的伤害+ {Layers} 点";
        public override BattlePerk Clone() => WithLayers(new BP_Strength(Config));
    }
}
