using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>施加效果词条：为指定目标施加一个状态（由 Perk 引用状态定义资产），层数由 Layers 决定。</summary>
    [Serializable]
    [AddTypeMenu("Affix/施加效果")]
    public sealed class AD_ApplyPerk : AffixData
    {
        [SerializeField] private SO_Perk _perk;
        [SerializeField] private int _layers;

        public SO_Perk Perk => _perk;
        public int Layers => _layers;
        public override string Name => "施加效果";
    }
}
