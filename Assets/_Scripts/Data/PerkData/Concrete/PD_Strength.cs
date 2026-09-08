using System;
using UnityEngine;
namespace _DreamKeep.Data
{
    /// <summary>状态「力量」：可施加数据，具体效果由 Domain 层结算。</summary>
    [Serializable]
    public class PD_Strength : PerkData
    {
        public override string Name => "力量";
    }
}
