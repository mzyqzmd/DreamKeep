using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「虚弱」：降低目标造成的伤害，回合结束层数递减。</summary>
    [Serializable]
    public class BP_Weak : BattlePerk
    {
        [SerializeField] private float _ratio = 0.25f;

        public override string Name => "虚弱";
        public float Ratio => _ratio;
        public override string GetDescription() => $"造成的伤害- {Ratio * 100} %";
        public override BattlePerk Clone() => WithLayers(new BP_Weak());
    }
}
