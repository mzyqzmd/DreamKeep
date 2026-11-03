using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>松动羊毛剪：拾起时先失去生命值（走基类代价字段），再从牌库中移除若干张牌。</summary>
    [CreateAssetMenu(fileName = "PrecariousShears", menuName = "DreamKeep/Data/Relic/松动羊毛剪")]
    public sealed class SO_PrecariousShears : SO_Relic
    {
        [SerializeField] private int _cardCount = 2;

        public int CardCount => _cardCount;
    }
}
