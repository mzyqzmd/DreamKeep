using System;
using UnityEngine;

namespace _DreamKeep.Data
{
    /// <summary>状态「易伤」：可施加数据，具体效果由 Domain 层结算。</summary>
    [Serializable]
    public class PD_Vulnerable : PerkData
    {
        [SerializeField] private float _ratio = 0.5f;
        public override string Name => "易伤";
        public float Ratio => _ratio;
    }
}
