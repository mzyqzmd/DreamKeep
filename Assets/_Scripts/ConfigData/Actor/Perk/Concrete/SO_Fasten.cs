using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「紧勒」定义：玩家常驻状态，名字含「防御」的牌额外获得等同层数的格挡，战斗内不自动衰减。</summary>
    [CreateAssetMenu(fileName = "Fasten", menuName = "DreamKeep/Data/Perk/紧勒")]
    public sealed class SO_Fasten : SO_Perk
    {
        public override string Name => "紧勒";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.None;
    }
}
