using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>失去生命词条：让目标失去指定数值的生命，无视格挡且不受伤害修正影响。</summary>
    [Serializable]
    [AddTypeMenu("Affix/失去生命")]
    public sealed class AD_LostHealth : AffixData
    {
        [SerializeField] private int _amount; // 失去的生命值

        public int Amount => _amount;
        public override string Name => "失去生命";
    }
}
