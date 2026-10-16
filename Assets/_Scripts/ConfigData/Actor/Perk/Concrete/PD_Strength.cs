using System;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「力量」配置模板：每层提高 1 点攻击伤害，战斗内不自动衰减。</summary>
    [Serializable]
    [AddTypeMenu("Perk/力量")]
    public sealed class PD_Strength : PerkData
    {
        public override string Name => "力量";
        public override EPerkDecayTiming DecayTiming => EPerkDecayTiming.None;
    }
}
