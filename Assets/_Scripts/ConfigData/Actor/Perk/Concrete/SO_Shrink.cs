using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「缩小」定义：降低宿主造成的伤害，回合结束层数递减。比率只在此配一次，所有引用点共用。</summary>
    [CreateAssetMenu(fileName = "Shrink", menuName = "DreamKeep/Data/Perk/缩小")]
    public sealed class SO_Shrink : SO_Perk
    {
        [SerializeField] private float _ratio = 0.3f;

        public float Ratio => _ratio;

        public override string Name => "缩小";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.TurnEnd;
    }
}
