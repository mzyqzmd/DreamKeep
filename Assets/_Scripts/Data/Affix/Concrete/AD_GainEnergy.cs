using System;
using UnityEngine;

namespace _DreamKeep.Data
{
    /// <summary>获得能量词条：为自身获得指定数值的能量。</summary>
    [Serializable]
    public class AD_GainEnergy : AffixData
    {
        [SerializeField] private int _amount;

        public int Amount => _amount;
        public override string Name => "获得能量";
        public override string Description => $"获得 {_amount} 点能量";
        public override EAffixKind Kind => EAffixKind.GainEnergy;
    }
}
