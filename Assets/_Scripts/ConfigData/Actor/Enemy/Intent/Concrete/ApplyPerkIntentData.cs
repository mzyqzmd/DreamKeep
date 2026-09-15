using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    [Serializable]
    public class ApplyPerkIntentData : IntentData
    {
        [SerializeReference] private BattlePerk _perk;
        [SerializeField] private int _layers;
        [SerializeField] private bool _isSelf;

        public BattlePerk Perk => _perk;
        public int Layers => _layers;
        public bool IsSelf => _isSelf;

        public override string GetDescription() =>
            _isSelf ? "这名敌人即将强化自己" : "这名敌人即将给予你一种负面效果";
    }
}