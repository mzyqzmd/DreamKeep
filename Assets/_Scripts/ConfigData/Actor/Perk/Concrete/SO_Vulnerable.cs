using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「易伤」定义：提高目标受到的伤害，回合结束层数递减。比率只在此配一次，所有引用点共用。</summary>
    [CreateAssetMenu(fileName = "Vulnerable", menuName = "DreamKeep/Data/Perk/易伤")]
    public sealed class SO_Vulnerable : SO_Perk
    {
        [SerializeField] private float _ratio = 0.5f;

        public float Ratio => _ratio;

        public override string Name => "易伤";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.TurnStart;
    }
}
