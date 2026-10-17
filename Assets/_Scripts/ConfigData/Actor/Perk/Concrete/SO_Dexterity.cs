using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「敏捷」定义：每层提高 1 点从卡牌中获得的格挡，层数为负即减少，战斗内不自动衰减。</summary>
    [CreateAssetMenu(fileName = "Dexterity", menuName = "DreamKeep/Data/Perk/敏捷")]
    public sealed class SO_Dexterity : SO_Perk
    {
        public override string Name => "敏捷";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.None;
    }
}
