using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「缓慢」定义：精英专属常驻状态，玩家每打出一张牌累积增伤，宿主回合结束清零且保留。</summary>
    [CreateAssetMenu(fileName = "Slow", menuName = "DreamKeep/Data/Perk/缓慢")]
    public sealed class SO_Slow : SO_Perk
    {
        [SerializeField] private int _percentPerCard = 10;

        public int PercentPerCard => _percentPerCard;

        public override string Name => "缓慢";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.TurnEnd;
    }
}
