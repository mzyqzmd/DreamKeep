using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>升级补丁·改数值：给某个值通道加一条修正（先加后乘，乘率默认 1 即只做加法）。
    /// 状态的施加层数不在值通道之列，由 UD_ModifyPerkLayers 表达。</summary>
    [Serializable]
    [AddTypeMenu("Upgrade/改数值")]
    public sealed class UD_ModifyValue : UpgradeData
    {
        [SerializeField] private ECardValue _value;
        [SerializeField] private float _additive; // 加值
        [SerializeField] private float _multiplier = 1f; // 乘率，升级一般不用

        public ECardValue Value => _value;
        public float Additive => _additive;
        public float Multiplier => _multiplier;
    }
}
