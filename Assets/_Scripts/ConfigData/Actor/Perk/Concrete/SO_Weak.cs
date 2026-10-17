using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「虚弱」定义：降低目标造成的伤害，回合结束层数递减。比率只在此配一次，所有引用点共用。</summary>
    [CreateAssetMenu(fileName = "Weak", menuName = "DreamKeep/Data/Perk/虚弱")]
    public sealed class SO_Weak : SO_Perk
    {
        [SerializeField] private float _ratio = 0.25f;

        public float Ratio => _ratio;

        public override string Name => "虚弱";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.TurnEnd;
    }
}
