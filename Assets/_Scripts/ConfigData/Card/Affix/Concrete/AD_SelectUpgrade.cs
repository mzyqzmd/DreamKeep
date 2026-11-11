using System;
using NaughtyAttributes;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>选择升级词条：本场战斗中升级手牌，档位可为全部或指定张数。</summary>
    [Serializable]
    [AddTypeMenu("Affix/选择升级")]
    public sealed class AD_SelectUpgrade : AffixData
    {
        [SerializeField] private ESelectScope _scope;

        //选择消耗张数(当选牌类型为ESelectScope.All时此字段无效)
        [SerializeField] private int _count;

        public ESelectScope Scope => _scope;
        public int Count => _count;
        public override string Name => "选择升级";
    }
}