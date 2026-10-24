using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>手里剑：同一回合内每打出 N 张攻击牌，获得指定状态（默认力量）的层数。</summary>
    [CreateAssetMenu(fileName = "Shuriken", menuName = "DreamKeep/Data/Relic/手里剑")]
    public sealed class SO_Shuriken : SO_Relic
    {
        [SerializeField] private int _cardCount = 3;
        [SerializeField] private SO_Strength _strength;
        [SerializeField] private int _layers = 1;

        public int CardCount => _cardCount;
        public SO_Strength Strength => _strength;
        public int Layers => _layers;
        public ECardType CardType => ECardType.Attack;
    }
}