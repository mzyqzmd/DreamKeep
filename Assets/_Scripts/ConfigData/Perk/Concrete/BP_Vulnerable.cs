using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「易伤」：提高目标受到的伤害，回合结束层数递减。</summary>
    [Serializable]
    [AddTypeMenu("Perk/易伤")]
    public class BP_Vulnerable : BattlePerk
    {
        public BP_Vulnerable(float ratio_) => Ratio = ratio_;
        public float Ratio { get; private set; }
        public override string Name => "易伤";
        public override string GetDescription() => $"受到的伤害+ {Ratio * 100} %";
        public override BattlePerk Clone() => WithLayers(new BP_Vulnerable(Ratio));
    }
}