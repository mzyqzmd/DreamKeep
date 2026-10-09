using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>抽牌词条：从抽牌堆抽指定张数的牌。</summary>
    [Serializable]
    [AddTypeMenu("Affix/抽牌")]
    public sealed class AD_DrawCard : AffixData
    {
        [SerializeField] private int _count;

        public int Count => _count;
        public override string Name => "抽牌";
    }
}
