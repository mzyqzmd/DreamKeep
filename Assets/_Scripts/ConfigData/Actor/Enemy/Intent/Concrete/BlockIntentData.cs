using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    [Serializable]
    [AddTypeMenu("Intent/格挡")]
    public sealed class BlockIntentData : IntentData
    {
        [SerializeField] private int _amount;
        public int Amount => _amount;
    }
}