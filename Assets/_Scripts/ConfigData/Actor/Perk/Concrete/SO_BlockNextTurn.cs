using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「下回合格挡」定义：宿主下个回合开始时获得等同层数的格挡，触发后整条移除。</summary>
    [CreateAssetMenu(fileName = "BlockNextTurn", menuName = "DreamKeep/Data/Perk/下回合格挡")]
    public sealed class SO_BlockNextTurn : SO_Perk
    {
        public override string Name => "下回合格挡";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.TurnStart;
    }
}
