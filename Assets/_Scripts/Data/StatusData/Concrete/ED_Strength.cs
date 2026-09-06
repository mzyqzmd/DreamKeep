using System;
using UnityEngine;
namespace _DreamKeep.Data
{
    /// <summary>状态「力量」：可施加数据，具体效果由 Domain 层结算。</summary>
    [Serializable]
    public class ED_Strength : EffectData
    {
        [SerializeField]private int _value;
        public override string Name => "力量";

        public override string Description => $"造成伤害基础数值提高 {_value} 点";
    }
}
