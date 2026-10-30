using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>怀表：本回合打出的牌不超过阈值时，下个玩家回合额外抽牌。</summary>
    [CreateAssetMenu(fileName = "PocketWatch", menuName = "DreamKeep/Data/Relic/怀表")]
    public sealed class SO_PocketWatch : SO_Relic
    {
        [SerializeField] private int _maxCards = 3;
        [SerializeField] private int _cards = 3;

        public int MaxCards => _maxCards;
        public int Cards => _cards;
    }
}
