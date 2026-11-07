using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「昏眩」定义：施加给玩家，本回合只能打出 1 张牌，宿主回合结束递减层数、归零移除。</summary>
    [CreateAssetMenu(fileName = "Dizzy", menuName = "DreamKeep/Data/Perk/昏眩")]
    public sealed class SO_Dizzy : SO_Perk
    {
        public override string Name => "昏眩";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.TurnEnd;
    }
}
