using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「仪式」定义：宿主回合开始时获得等同层数的力量，常驻不衰减。</summary>
    [CreateAssetMenu(fileName = "Ritual", menuName = "DreamKeep/Data/Perk/仪式")]
    public sealed class SO_Ritual : SO_Perk
    {
        [SerializeField] private SO_Strength _strength;

        public SO_Strength Strength => _strength;

        public override string Name => "仪式";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.TurnStart;
    }
}
