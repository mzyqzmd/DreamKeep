using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>治疗词条：让目标恢复指定数值的生命，满血时钳到上限，不受伤害修正影响。</summary>
    [Serializable]
    [AddTypeMenu("Affix/治疗")]
    public sealed class AD_Heal : AffixData
    {
        [SerializeField] private int _amount; // 恢复的生命值

        public int Amount => _amount;
        public override string Name => "治疗";
    }
}
