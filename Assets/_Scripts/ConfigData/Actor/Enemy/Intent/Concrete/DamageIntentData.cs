using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    [Serializable]
    public class DamageIntentData : IntentData
    {
        [SerializeField] private int _damage;
        [SerializeField] private int _times;

        public int Damage => _damage;
        public int Times => _times;
        public override string GetDescription() => $"这名敌人即将对你造成伤害";
    }
}