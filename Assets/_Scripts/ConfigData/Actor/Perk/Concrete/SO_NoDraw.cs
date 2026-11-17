using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「禁止抽牌」定义：本回合内不能再抽牌，回合结束自动移除。</summary>
    [CreateAssetMenu(fileName = "NoDraw", menuName = "DreamKeep/Data/Perk/禁止抽牌")]
    public sealed class SO_NoDraw : SO_Perk
    {
        public override string Name => "禁止抽牌";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.TurnEnd;
    }
}
