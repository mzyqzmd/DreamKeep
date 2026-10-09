using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>治疗意图配置：恢复自己指定数值的生命。</summary>
    [Serializable]
    [AddTypeMenu("Intent/治疗")]
    public sealed class HealIntentData : IntentData
    {
        [SerializeField] private int _amount;

        public int Amount => _amount;
    }
}
