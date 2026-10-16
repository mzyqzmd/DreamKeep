using System;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「再生」配置模板：回合结束恢复等于层数的生命，随后层数递减。</summary>
    [Serializable]
    [AddTypeMenu("Perk/再生")]
    public sealed class PD_Regen : PerkData
    {
        public override string Name => "再生";
        public override EPerkDecayTiming DecayTiming => EPerkDecayTiming.TurnEnd;
    }
}
