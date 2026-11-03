using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>支出金币：选中即从整局金币扣除，金额不足的置灰由选项条件（GoldAtLeast）兜住。</summary>
    [Serializable]
    [AddTypeMenu("Event/支出金币")]
    public sealed class EED_SpendGold : EventEffectData
    {
        [SerializeField] private int _amount; // 支出的金币数

        public int Amount => _amount;
    }
}
