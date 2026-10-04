using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    [Serializable]
    [AddTypeMenu("Intent/攻击")]
    public class DamageIntentData : IntentData
    {
        [SerializeField] private int _damage;
        [SerializeField] private int _times;

        public int Damage => _damage;
        public int Times => _times;
    }
}