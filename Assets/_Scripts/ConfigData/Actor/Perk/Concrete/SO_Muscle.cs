using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「肌肉」定义：层数即同步给宿主的真实力量层，回合结束整条移除并扣回。</summary>
    [CreateAssetMenu(fileName = "Muscle", menuName = "DreamKeep/Data/Perk/肌肉")]
    public sealed class SO_Muscle : SO_Perk
    {
        [SerializeField] private SO_Strength _strength;

        public SO_Strength Strength => _strength;

        public override string Name => "肌肉";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.TurnEnd;
    }
}
