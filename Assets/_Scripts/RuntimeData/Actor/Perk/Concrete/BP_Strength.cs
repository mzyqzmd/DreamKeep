using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「力量」运行时实例：每层提高 1 点攻击伤害（供伤害管线的加性阶段读取），战斗内不自动衰减。</summary>
    public sealed class BP_Strength : BattlePerk
    {
        private SO_Strength Config => (SO_Strength)_data;

        public BP_Strength(SO_Strength data_) : base(data_) { }

        /// <summary>加性贡献：宿主自己出手时每层 +1。</summary>
        public override int ModifyDamageAdditive(Creature host_, Creature source_, Creature target_) =>
            host_ == source_ ? Layers : 0;

        public override string GetDescription() => $"从卡牌中造成的伤害+ {Layers} 点";
        public override BattlePerk Clone() => WithLayers(new BP_Strength(Config));
    }
}
