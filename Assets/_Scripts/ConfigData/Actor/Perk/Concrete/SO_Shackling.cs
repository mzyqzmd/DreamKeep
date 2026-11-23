using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「镣铐」定义：层数为负，表示从宿主身上拿走的真实力量层，回合结束整条移除并加回。</summary>
    [CreateAssetMenu(fileName = "Shackling", menuName = "DreamKeep/Data/Perk/镣铐")]
    public sealed class SO_Shackling : SO_Perk
    {
        [SerializeField] private SO_Strength _strength;

        public SO_Strength Strength => _strength;

        public override string Name => "镣铐";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.TurnEnd;
    }
}
