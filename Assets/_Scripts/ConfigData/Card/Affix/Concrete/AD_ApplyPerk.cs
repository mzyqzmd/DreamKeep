using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>施加效果词条：为指定目标施加一个状态（由 Status 引用具体状态实例），层数由 Layers 决定。</summary>
    [Serializable]
    [AddTypeMenu("Affix/施加效果")]
    public class AD_ApplyPerk : AffixData
    {
        [SerializeReference] private BattlePerk _perk;
        [SerializeField] private int _layers;

        public BattlePerk Perk => _perk;
        public int Layers => _layers;
        public override string Name => "施加效果";
    }
}
