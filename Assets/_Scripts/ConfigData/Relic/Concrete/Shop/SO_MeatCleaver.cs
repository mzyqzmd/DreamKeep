using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>切肉刀：持有后在休息处解锁「烹饪」选项——从牌库中删除若干张牌，并提高等量配置的最大生命。
    /// 本身不提供任何战斗效果，烹饪资格与数值消费由 RestEntry 判定与执行。</summary>
    [CreateAssetMenu(fileName = "MeatCleaver", menuName = "DreamKeep/Data/Relic/切肉刀")]
    public sealed class SO_MeatCleaver : SO_Relic
    {
        [Min(1)]
        [SerializeField] private int _removeCards = 2;

        [Min(1)]
        [SerializeField] private int _hpGain = 5;

        /// <summary>每次烹饪从牌库中删除的卡牌张数。</summary>
        public int RemoveCards => _removeCards;

        /// <summary>每次烹饪提高的最大生命值。</summary>
        public int HpGain => _hpGain;
    }
}
