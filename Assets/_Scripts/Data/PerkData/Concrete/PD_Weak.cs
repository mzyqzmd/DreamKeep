using System;
using UnityEngine;

namespace _DreamKeep.Data
{
    /// <summary>状态「虚弱」：可施加数据，具体效果由 Domain 层结算。</summary>
    [Serializable]
    public class PD_Weak : PerkData
    {
        [SerializeField] private float _ratio = 0.25f;
        public override string Name => "虚弱";
        public float Ratio => _ratio;
    }
}
