using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「力量」定义：每层提高 1 点攻击伤害，战斗内不自动衰减。</summary>
    [CreateAssetMenu(fileName = "Strength", menuName = "DreamKeep/Data/Perk/力量")]
    public sealed class SO_Strength : SO_Perk
    {
        public override string Name => "力量";
        public override EPerkDecayTiming DecayTiming => EPerkDecayTiming.None;
    }
}
