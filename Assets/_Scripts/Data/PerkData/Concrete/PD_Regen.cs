using System;
using UnityEngine;

namespace _DreamKeep.Data
{
    /// <summary>状态「再生」：可施加数据，具体效果由 Domain 层结算。</summary>
    [Serializable]
    public class PD_Regen : PerkData
    {
        public override string Name => "再生";
    }
}
