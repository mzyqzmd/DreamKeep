using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「领地意识」定义：精英专属常驻状态，宿主自身回合结束时按层数获得力量。</summary>
    [CreateAssetMenu(fileName = "Territorial", menuName = "DreamKeep/Data/Perk/领地意识")]
    public sealed class SO_Territorial : SO_Perk
    {
        [SerializeField] private SO_Strength _strength;

        public SO_Strength Strength => _strength;

        public override string Name => "领地意识";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.TurnEnd;
    }
}
