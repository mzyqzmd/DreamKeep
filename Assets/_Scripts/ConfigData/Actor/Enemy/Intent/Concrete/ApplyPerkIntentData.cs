using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    [Serializable]
    [AddTypeMenu("Intent/施加效果")]
    public sealed class ApplyPerkIntentData : IntentData
    {
        [SerializeField] private SO_Perk _perk;
        [SerializeField] private int _layers;
        [SerializeField] private bool _isSelf;

        public SO_Perk Perk => _perk;
        public int Layers => _layers;
        public bool IsSelf => _isSelf;
    }
}