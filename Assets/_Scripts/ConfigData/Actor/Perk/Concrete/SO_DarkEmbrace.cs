using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「黑暗之拥」定义：每当有一张牌被消耗时抽牌，张数即层数，战斗内不自动衰减。</summary>
    [CreateAssetMenu(fileName = "DarkEmbrace", menuName = "DreamKeep/Data/Perk/黑暗之拥")]
    public sealed class SO_DarkEmbrace : SO_Perk
    {
        public override string Name => "黑暗之拥";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.None;
    }
}
