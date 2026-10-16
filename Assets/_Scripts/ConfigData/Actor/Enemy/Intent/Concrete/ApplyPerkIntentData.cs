using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    [Serializable]
    [AddTypeMenu("Intent/施加效果")]
    public sealed class ApplyPerkIntentData : IntentData
    {
        [SerializeReference, SubclassSelector] private PerkData _perk;
        [SerializeField] private int _layers;
        [SerializeField] private bool _isSelf;

        public PerkData Perk => _perk;
        public int Layers => _layers;
        public bool IsSelf => _isSelf;
    }
}