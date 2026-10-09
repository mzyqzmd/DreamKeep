using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    [Serializable]
    [AddTypeMenu("Intent/混合意图(最多两个混合)")]
    public sealed class MixedIntentData : IntentData
    {
        //NOTE:最多同时有两个意图
        [SerializeReference, SubclassSelector] private IntentData _intent1;
        [SerializeReference, SubclassSelector] private IntentData _intent2;

        public IntentData Intent1 => _intent1;
        public IntentData Intent2 => _intent2;
    }
}