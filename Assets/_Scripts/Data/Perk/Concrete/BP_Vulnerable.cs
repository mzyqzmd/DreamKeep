using System;
using UnityEngine;

namespace _DreamKeep.Data
{
    /// <summary>状态「易伤」：提高目标受到的伤害，回合结束层数递减。</summary>
    [Serializable]
    public class BP_Vulnerable : BattlePerk
    {
        [SerializeField] private float _ratio = 0.5f;

        public override string Name => "易伤";
        public float Ratio => _ratio;
        public override string GetDescription() => $"受到的伤害提高 {Ratio * 100} %";
        public override BattlePerk Clone() => WithLayers(new BP_Vulnerable());
    }
}
