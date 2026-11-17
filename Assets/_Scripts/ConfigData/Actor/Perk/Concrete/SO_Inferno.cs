using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「狱火」定义：宿主回合开始失去生命，且回合内每次失去生命对所有敌人造成等同层数的伤害，常驻不衰减。</summary>
    [CreateAssetMenu(fileName = "Inferno", menuName = "DreamKeep/Data/Perk/狱火")]
    public sealed class SO_Inferno : SO_Perk
    {
        [SerializeField] private int _hpCost = 1;

        public int HpCost => _hpCost;

        public override string Name => "狱火";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.TurnStart;
    }
}
