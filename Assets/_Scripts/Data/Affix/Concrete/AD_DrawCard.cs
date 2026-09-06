using System;
using UnityEngine;

namespace _DreamKeep.Data
{
    /// <summary>抽牌词条：从抽牌堆抽指定张数的牌。</summary>
    [Serializable]
    public class AD_DrawCard : AffixData
    {
        [SerializeField] private int _count;

        public int Count => _count;
        public override string Name => "抽牌";
        public override string Description => $"抽 {_count} 张牌";
        public override EAffixKind Kind => EAffixKind.DrawCard;
    }
}
