using System;
using System.Text;
using UnityEngine;

namespace _DreamKeep.Data
{
    [Serializable]
    public class MixedIntentData : IntentData
    {
        //NOTE:最多同时有两个意图
        [SerializeReference] private IntentData _intent1;
        [SerializeReference] private IntentData _intent2;

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