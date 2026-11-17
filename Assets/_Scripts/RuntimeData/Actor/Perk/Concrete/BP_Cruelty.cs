using _DreamKeep.ConfigData;
using _DreamKeep.Utility;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「残酷」运行时实例：宿主对有易伤状态的目标造成伤害时按层数百分比增伤，战斗内不衰减。</summary>
    public sealed class BP_Cruelty : BattlePerk
    {
        private SO_Cruelty Config => (SO_Cruelty)_data;

        public BP_Cruelty(SO_Cruelty data_) : base(data_) { }

        /// <summary>乘性贡献：宿主自己出手且目标带易伤时提高为 ×(1+Layers/100)。</summary>
        public StatModifier GetModifier(EStatChannel channel_, Creature host_, Creature source_, Creature target_) =>
            channel_ == EStatChannel.Damage && host_ == source_ && BattlePerkQuery.Has<BP_Vulnerable>(target_)
                ? new StatModifier(0, 1f + Layers / 100f)
                : StatModifier.Neutral;

        public override string GetDescription() => GetDescription(null, Layers);

        public override string GetDescription(IStatRenderer valueRenderer_, int baseLayers_) =>
            $"有易伤状态的敌人\n额外受到 {RenderLayers(valueRenderer_, baseLayers_)} % 伤害";

        public override BattlePerk Clone() => WithLayers(new BP_Cruelty(Config));
    }
}
