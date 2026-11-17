using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「壁垒」定义：玩家常驻状态，回合开始不再清除格挡，战斗内不自动衰减。</summary>
    [CreateAssetMenu(fileName = "Barricade", menuName = "DreamKeep/Data/Perk/壁垒")]
    public sealed class SO_Barricade : SO_Perk
    {
        public override string Name => "壁垒";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.None;
    }
}
