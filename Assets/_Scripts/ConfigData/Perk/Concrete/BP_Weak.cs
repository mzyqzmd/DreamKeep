using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「虚弱」：降低目标造成的伤害，回合结束层数递减。</summary>
    [Serializable]
    [AddTypeMenu("Perk/虚弱")]
    public class BP_Weak : BattlePerk
    {
        [SerializeField] private float _ratio = 0.25f;
        public float Ratio => _ratio;
        public override string Name => "虚弱";
        public override EPerkDecayTiming DecayTiming => EPerkDecayTiming.TurnEnd;
        public override string GetDescription() => $"接下来 {Layers} 回合内，造成的伤害 - {Ratio * 100} %";
        public override BattlePerk Clone() => WithLayers(new BP_Weak());
    }
}