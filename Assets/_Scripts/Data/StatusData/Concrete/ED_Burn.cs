using System;
using UnityEngine;

namespace _DreamKeep.Data
{
    /// <summary>状态「灼烧」：可施加数据，具体效果由 Domain 层结算。</summary>
    [Serializable]
    public class ED_Burn : EffectData
    {
        [SerializeField] private int _value;
        public override string Name => "灼烧";

        public override string Description => $"回合结束时受到 {_value} 点伤害";
    }
}
