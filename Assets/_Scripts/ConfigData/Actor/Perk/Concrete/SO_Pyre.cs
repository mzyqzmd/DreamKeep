using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「薪火之源」定义：层数即玩家能量上限的加成，常驻整场战斗、不参与回合结算。</summary>
    [CreateAssetMenu(fileName = "Pyre", menuName = "DreamKeep/Data/Perk/薪火之源")]
    public sealed class SO_Pyre : SO_Perk
    {
        public override string Name => "薪火之源";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.None;
    }
}
