using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「虚弱」配置模板：降低目标造成的伤害，回合结束层数递减。</summary>
    [Serializable]
    [AddTypeMenu("Perk/虚弱")]
    public sealed class PD_Weak : PerkData
    {
        [SerializeField] private float _ratio = 0.25f;
        public float Ratio => _ratio;
        public override string Name => "虚弱";
        public override EPerkDecayTiming DecayTiming => EPerkDecayTiming.TurnEnd;
    }
}
