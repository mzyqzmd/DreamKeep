using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「复制」定义：宿主下一张牌额外打出一次，触发后整条移除，宿主回合结束时失效。</summary>
    [CreateAssetMenu(fileName = "Duplication", menuName = "DreamKeep/Data/Perk/复制")]
    public sealed class SO_Duplication : SO_Perk
    {
        public override string Name => "复制";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.TurnEnd;
    }
}
