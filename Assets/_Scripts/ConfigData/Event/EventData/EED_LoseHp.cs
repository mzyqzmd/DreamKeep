using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>失去生命：无视格挡的事件扣血，致死由选项条件（HpAbove：当前生命 > 扣血值）置灰兜住，不靠封顶。</summary>
    [Serializable]
    [AddTypeMenu("Event/失去生命")]
    public sealed class EED_LoseHp : EventEffectData
    {
        [SerializeField] private int _amount; // 失去的生命值

        public int Amount => _amount;
    }
}
