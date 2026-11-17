using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「凶恶」定义：每当宿主给予易伤时抽牌，张数即层数，战斗内不自动衰减。</summary>
    [CreateAssetMenu(fileName = "Vicious", menuName = "DreamKeep/Data/Perk/凶恶")]
    public sealed class SO_Vicious : SO_Perk
    {
        public override string Name => "凶恶";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.None;
    }
}
