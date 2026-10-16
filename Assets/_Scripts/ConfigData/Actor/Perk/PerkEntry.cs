using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>初始状态条目：状态定义资产与出场层数——定义不携带层数，层数只能随条目配置（同 ApplyPerkIntentData 的 _perk/_layers 先例）。</summary>
    [Serializable]
    public class PerkEntry
    {
        [SerializeField] private SO_Perk _perk;
        [SerializeField] private int _layers;

        public SO_Perk Perk => _perk;
        public int Layers => _layers;
    }
}
