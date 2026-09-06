using System;
using UnityEngine;

namespace _DreamKeep.Data
{
    /// <summary>状态「虚弱」：可施加数据，具体效果由 Domain 层结算。</summary>
    [Serializable]
    public class ED_Weak : EffectData
    {
        public override string Name => "虚弱";

        public override string Description => "造成的伤害降低 25%";
    }
}
