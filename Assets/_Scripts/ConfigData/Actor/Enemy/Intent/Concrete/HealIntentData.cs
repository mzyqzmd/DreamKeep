using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>治疗意图配置：恢复自己指定数值的生命。</summary>
    [Serializable]
    [AddTypeMenu("Intent/治疗")]
    public class HealIntentData : IntentData
    {
        [SerializeField] private int _amount;

        public int Amount => _amount;
        public override string GetDescription() => $"这名敌人即将治疗自己";
    }
}
