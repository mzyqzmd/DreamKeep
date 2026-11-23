using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「稳定血清」定义：宿主接下来若干回合的回合末保留全部手牌，在回合开始时递减一层。
    /// 刻意选 TurnStart 结算：回合末弃牌筛选要读到递减前的层数，才能兑现"保留 N 个回合"。</summary>
    [CreateAssetMenu(fileName = "StableSerum", menuName = "DreamKeep/Data/Perk/稳定血清")]
    public sealed class SO_StableSerum : SO_Perk
    {
        public override string Name => "稳定血清";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.TurnStart;
    }
}
