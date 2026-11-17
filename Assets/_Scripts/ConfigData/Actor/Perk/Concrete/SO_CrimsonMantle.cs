using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「绯红披风」定义：回合开始时先失去 1 点生命再获得等同层数的格挡，常驻不衰减。</summary>
    [CreateAssetMenu(fileName = "CrimsonMantle", menuName = "DreamKeep/Data/Perk/绯红披风")]
    public sealed class SO_CrimsonMantle : SO_Perk
    {
        [SerializeField] private int _hpCost = 1;

        public int HpCost => _hpCost;

        public override string Name => "绯红披风";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.TurnStart;
    }
}
