using System;
using UnityEngine;

namespace _DreamKeep.Data
{
    /// <summary>施加状态词条：为指定目标施加一个状态（由 Status 引用具体状态实例），层数由 Layers 决定。</summary>
    [Serializable]
    public class AD_ApplyPerk : AffixData
    {
        [SerializeReference] private PerkData _perk;
        [SerializeField] private int _layers;

        public PerkData Perk => _perk;
        public int Layers => _layers;
        public override string Name => "施加状态";
    }
}
