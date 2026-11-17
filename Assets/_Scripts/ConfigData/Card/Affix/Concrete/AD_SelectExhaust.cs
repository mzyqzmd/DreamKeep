using System;
using NaughtyAttributes;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>选择消耗词条：消耗手牌，档位可为全部、恰好指定张数、至多指定张数或任意张。</summary>
    [Serializable]
    [AddTypeMenu("Affix/选择消耗")]
    public sealed class AD_SelectExhaust : AffixData
    {
        [SerializeField] private ESelectScope _scope;

        //选择消耗张数(AllHand与Any档忽略此字段)
        [SerializeField] private int _count;

        public ESelectScope Scope => _scope;
        public int Count => _count;
        public override string Name => "选择消耗";
    }
}