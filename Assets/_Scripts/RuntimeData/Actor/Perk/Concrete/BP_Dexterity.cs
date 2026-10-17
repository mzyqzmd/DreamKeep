using _DreamKeep.ConfigData;
using _DreamKeep.Utility;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「敏捷」运行时实例：每层提高 1 点从卡牌中获得的格挡（供 Hook 分派表读取），
    /// 层数为负即减少；战斗内不自动衰减。</summary>
    public sealed class BP_Dexterity : BattlePerk
    {
        private SO_Dexterity Config => (SO_Dexterity)_data;

        public BP_Dexterity(SO_Dexterity data_) : base(data_) { }

        /// <summary>加性贡献：宿主自己获得格挡时每层 +1（负层即减）。</summary>
        public StatModifier GetModifier(EStatChannel channel_, Creature host_, Creature source_, Creature target_) =>
            channel_ == EStatChannel.Block && host_ == source_ ? new StatModifier(Layers) : StatModifier.Neutral;

        public override string GetDescription() => $"从卡牌中获得的格挡+ {Layers} 点";
        public override BattlePerk Clone() => WithLayers(new BP_Dexterity(Config));
    }
}
