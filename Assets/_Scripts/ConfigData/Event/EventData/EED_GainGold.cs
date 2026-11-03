using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>获得金币：单项目奖励直接入账（RunState.GainGold），多项奖励才由会话改走奖励面板。</summary>
    [Serializable]
    [AddTypeMenu("Event/获得金币")]
    public sealed class EED_GainGold : EventEffectData
    {
        [SerializeField] private int _amount; // 获得的金币数

        public int Amount => _amount;
    }
}
