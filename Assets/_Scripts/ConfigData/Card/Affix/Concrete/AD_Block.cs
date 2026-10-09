using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>格挡词条：为自身获得指定数值的格挡。</summary>
    [Serializable]
    [AddTypeMenu("Affix/格挡")]
    public sealed class AD_Block : AffixData
    {
        [SerializeField] private int _amount;

        public int Amount => _amount;
        public override string Name => "格挡";
    }
}
