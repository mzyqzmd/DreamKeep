using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>伤害词条：对目标造成指定数值的伤害。</summary>
    [Serializable]
    [AddTypeMenu("Affix/伤害")]
    public sealed class AD_Damage : AffixData
    {
        [SerializeField] private int _amount; // 伤害值
        [SerializeField] private int _times;  // 伤害次数

        public int Amount => _amount;
        public int Times => _times;
        public override string Name => "伤害";
    }
}
