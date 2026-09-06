using System;
using UnityEngine;

namespace _DreamKeep.Data
{
    /// <summary>状态「再生」：可施加数据，具体效果由 Domain 层结算。</summary>
    [Serializable]
    public class ED_Regen : EffectData
    {
        [SerializeField]private int _value;
        public override string Name => "再生";

        public override string Description => $"回合结束时回复 {_value} 点生命值并减少 1 层再生";
    }
}
