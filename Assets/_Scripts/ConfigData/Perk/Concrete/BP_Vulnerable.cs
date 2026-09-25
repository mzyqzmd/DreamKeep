using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「易伤」：提高目标受到的伤害，回合结束层数递减。</summary>
    [Serializable]
    [AddTypeMenu("Perk/易伤")]
    public class BP_Vulnerable : BattlePerk
    {
        [SerializeField] private float _ratio = 0.5f;
        public float Ratio => _ratio;
        public override string Name => "易伤";
        public override EPerkDecayTiming DecayTiming => EPerkDecayTiming.TurnEnd;
        public override string GetDescription() => $"接下来 {Layers} 回合内，受到的伤害+ {Ratio * 100} %";
        public override BattlePerk Clone() => WithLayers(new BP_Vulnerable());
    }
}