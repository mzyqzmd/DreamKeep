using System;
using NaughtyAttributes;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>选择升级词条：本场战斗中升级手牌，档位可为全部、恰好指定张数、至多指定张数或任意张。</summary>
    [Serializable]
    [AddTypeMenu("Affix/选择升级")]
    public sealed class AD_SelectUpgrade : AffixData
    {
        [SerializeField] private ESelectScope _scope;

        //选择消耗张数(AllHand与Any档忽略此字段)
        [SerializeField] private int _count;

        public ESelectScope Scope => _scope;
        public int Count => _count;
        public override string Name => "选择升级";
    }
}