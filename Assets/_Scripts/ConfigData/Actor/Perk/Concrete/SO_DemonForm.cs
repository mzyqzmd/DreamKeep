using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「恶魔形态」定义：宿主回合开始时获得等同层数的力量，常驻不衰减。</summary>
    [CreateAssetMenu(fileName = "DemonForm", menuName = "DreamKeep/Data/Perk/恶魔形态")]
    public sealed class SO_DemonForm : SO_Perk
    {
        [SerializeField] private SO_Strength _strength;

        public SO_Strength Strength => _strength;

        public override string Name => "恶魔形态";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.TurnStart;
    }
}
