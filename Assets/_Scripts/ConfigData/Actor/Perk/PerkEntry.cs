using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>初始状态条目：perk 模板与出场层数——BattlePerk.Layers 是私有 setter、配置模板恒 0，层数只能随条目配置（同 ApplyPerkIntentData 的 _perk/_layers 先例）。</summary>
    [Serializable]
    public class PerkEntry
    {
        [SerializeReference, SubclassSelector] private BattlePerk _perk;
        [SerializeField] private int _layers;

        public BattlePerk Perk => _perk;
        public int Layers => _layers;
    }
}
