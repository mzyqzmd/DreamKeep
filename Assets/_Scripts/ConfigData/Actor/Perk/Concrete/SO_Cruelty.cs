using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「残酷」定义：宿主对有易伤状态的目标造成伤害时按层数百分比增伤，战斗内不自动衰减。</summary>
    [CreateAssetMenu(fileName = "Cruelty", menuName = "DreamKeep/Data/Perk/残酷")]
    public sealed class SO_Cruelty : SO_Perk
    {
        public override string Name => "残酷";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.None;
    }
}
