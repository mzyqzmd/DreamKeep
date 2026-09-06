using System;
using UnityEngine;

namespace _DreamKeep.Data
{
    /// <summary>格挡词条：为自身获得指定数值的格挡。</summary>
    [Serializable]
    public class AD_Block : AffixData
    {
        [SerializeField] private int _amount;

        public int Amount => _amount;
        public override string Name => "格挡";
        public override string Description => $"获得 {_amount} 点格挡";
        public override EAffixKind Kind => EAffixKind.Block;
    }
}
