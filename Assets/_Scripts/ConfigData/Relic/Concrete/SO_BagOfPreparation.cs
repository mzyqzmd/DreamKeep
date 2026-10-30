using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>准备背包：战斗首回合的初始抽牌之外额外抽牌。</summary>
    [CreateAssetMenu(fileName = "BagOfPreparation", menuName = "DreamKeep/Data/Relic/准备背包")]
    public sealed class SO_BagOfPreparation : SO_Relic
    {
        [SerializeField] private int _cards = 2;

        public int Cards => _cards;
    }
}
