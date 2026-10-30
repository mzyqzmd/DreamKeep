using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>金纸：每消耗若干张牌就抽牌（全战斗累计）。</summary>
    [CreateAssetMenu(fileName = "GoldPaper", menuName = "DreamKeep/Data/Relic/金纸")]
    public sealed class SO_GoldPaper : SO_Relic
    {
        [SerializeField] private int _count = 3;
        [SerializeField] private int _cards = 1;

        public int Count => _count;
        public int Cards => _cards;
    }
}
