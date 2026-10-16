using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「中毒」定义：回合结束时受到等于层数的伤害，随后层数递减。</summary>
    [CreateAssetMenu(fileName = "Poison", menuName = "DreamKeep/Data/Perk/中毒")]
    public sealed class SO_Poison : SO_Perk
    {
        public override string Name => "中毒";
        public override EPerkDecayTiming DecayTiming => EPerkDecayTiming.TurnStart;
    }
}