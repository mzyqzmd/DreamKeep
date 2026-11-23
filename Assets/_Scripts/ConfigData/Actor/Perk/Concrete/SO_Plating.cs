using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「覆甲」定义：宿主回合结束时获得等同层数的格挡，随后层数递减。</summary>
    [CreateAssetMenu(fileName = "Plating", menuName = "DreamKeep/Data/Perk/覆甲")]
    public sealed class SO_Plating : SO_Perk
    {
        public override string Name => "覆甲";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.TurnEnd;
    }
}
