using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>获得能量词条：为自身获得指定数值的能量。</summary>
    [Serializable]
    public class AD_GainEnergy : AffixData
    {
        [SerializeField] private int _amount;

        public int Amount => _amount;
        public override string Name => "获得能量";
    }
}
