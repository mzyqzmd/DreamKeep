using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「易伤」配置模板：提高目标受到的伤害，回合结束层数递减。</summary>
    [Serializable]
    [AddTypeMenu("Perk/易伤")]
    public sealed class PD_Vulnerable : PerkData
    {
        [SerializeField] private float _ratio = 0.5f;
        public float Ratio => _ratio;
        public override string Name => "易伤";
        public override EPerkDecayTiming DecayTiming => EPerkDecayTiming.TurnEnd;
    }
}
