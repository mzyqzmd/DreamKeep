using System;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「力量」：每层提高 1 点攻击伤害，战斗内不自动衰减。</summary>
    [Serializable]
    [AddTypeMenu("Perk/力量")]
    public sealed class BP_Strength : BattlePerk
    {
        public override string Name => "力量";
        public override EPerkDecayTiming DecayTiming => EPerkDecayTiming.None;
        public override string GetDescription() => $"从卡牌中造成的伤害+ {Layers} 点";
        public override BattlePerk Clone() => WithLayers(new BP_Strength());
    }
}
