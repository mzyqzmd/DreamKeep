using System;
using System.Text;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    [Serializable]
    [AddTypeMenu("Intent/混合意图(最多两个混合)")]
    public class MixedIntentData : IntentData
    {
        //NOTE:最多同时有两个意图
        [SerializeReference, SubclassSelector] private IntentData _intent1;
        [SerializeReference, SubclassSelector] private IntentData _intent2;

        public IntentData Intent1 => _intent1;
        public IntentData Intent2 => _intent2;

        public override string GetDescription()
        {
            StringBuilder description = new();
            description.AppendLine(_intent1.GetDescription());
            description.AppendLine(_intent2.GetDescription());
            return description.ToString();
        }
    }
}