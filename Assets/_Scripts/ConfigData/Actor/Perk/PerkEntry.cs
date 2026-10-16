using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>初始状态条目：perk 配置模板与出场层数——模板不携带层数，层数只能随条目配置（同 ApplyPerkIntentData 的 _perk/_layers 先例）。</summary>
    [Serializable]
    public class PerkEntry
    {
        [SerializeReference, SubclassSelector] private PerkData _perk;
        [SerializeField] private int _layers;

        public PerkData Perk => _perk;
        public int Layers => _layers;
    }
}
