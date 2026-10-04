using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    [Serializable]
    [AddTypeMenu("Intent/施加效果")]
    public class ApplyPerkIntentData : IntentData
    {
        [SerializeReference, SubclassSelector] private BattlePerk _perk;
        [SerializeField] private int _layers;
        [SerializeField] private bool _isSelf;

        public BattlePerk Perk => _perk;
        public int Layers => _layers;
        public bool IsSelf => _isSelf;
    }
}