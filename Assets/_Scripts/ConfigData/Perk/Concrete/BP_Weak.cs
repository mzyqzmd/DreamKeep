using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「虚弱」：降低目标造成的伤害，回合结束层数递减。</summary>
    [Serializable]
    [AddTypeMenu("Perk/虚弱")]
    public class BP_Weak : BattlePerk
    {
        public BP_Weak(float ratio_) => Ratio = ratio_;
        public float Ratio { get; private set; }
        public override string Name => "虚弱";
        public override string GetDescription() => $"造成的伤害 - {Ratio * 100} %";
        public override BattlePerk Clone() => WithLayers(new BP_Weak(Ratio));
    }
}