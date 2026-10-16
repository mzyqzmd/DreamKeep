using System;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「灼烧」配置模板：回合结束时受到等于层数的伤害，随后层数递减。</summary>
    [Serializable]
    [AddTypeMenu("Perk/灼烧")]
    public sealed class PD_Burn : PerkData
    {
        public override string Name => "灼烧";
        public override EPerkDecayTiming DecayTiming => EPerkDecayTiming.TurnEnd;
    }
}
