using System;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「灼烧」：回合结束时受到等于层数的伤害，随后层数递减。</summary>
    [Serializable]
    [AddTypeMenu("Perk/灼烧")]
    public class BP_Burn : BattlePerk
    {
        public override string Name => "灼烧";
        public override EPerkDecayTiming DecayTiming => EPerkDecayTiming.TurnEnd;
        public override string GetDescription() => $"回合结束受到 {Layers} 点伤害，随后层数-1";
        public override BattlePerk Clone() => WithLayers(new BP_Burn());
    }
}