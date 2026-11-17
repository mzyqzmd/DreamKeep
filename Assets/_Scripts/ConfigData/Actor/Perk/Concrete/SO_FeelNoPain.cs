using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「无惧疼痛」定义：每当有一张牌被消耗时获得等同层数的格挡，战斗内不自动衰减。</summary>
    [CreateAssetMenu(fileName = "FeelNoPain", menuName = "DreamKeep/Data/Perk/无惧疼痛")]
    public sealed class SO_FeelNoPain : SO_Perk
    {
        public override string Name => "无惧疼痛";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.None;
    }
}
