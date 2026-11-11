using System;
using NaughtyAttributes;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>选择消耗词条：消耗手牌，档位可为全部或至多指定张数。</summary>
    [Serializable]
    [AddTypeMenu("Affix/选择消耗")]
    public sealed class AD_SelectExhaust : AffixData
    {
        [SerializeField] private ESelectScope _scope;

        //选择消耗张数(当选牌类型为ESelectScope.All时此字段无效)
        [SerializeField] private int _count;

        public ESelectScope Scope => _scope;
        public int Count => _count;
        public override string Name => "选择消耗";
    }
}