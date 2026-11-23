using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「明晰」定义：宿主接下来若干回合开始时额外抽 1 张牌，每回合递减一层。</summary>
    [CreateAssetMenu(fileName = "ClarityPerk", menuName = "DreamKeep/Data/Perk/明晰")]
    public sealed class SO_ClarityPerk : SO_Perk
    {
        public override string Name => "明晰";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.TurnStart;
    }
}
