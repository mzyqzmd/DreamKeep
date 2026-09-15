using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    [Serializable]
    public class BlockIntentData : IntentData
    {
        [SerializeField] private int _amount;
        public int Amount => _amount;
        public override string GetDescription() => "这名敌人即将让自己获得格挡";
    }
}