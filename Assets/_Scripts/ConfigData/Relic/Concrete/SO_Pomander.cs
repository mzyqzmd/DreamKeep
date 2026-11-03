using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>橙型香盒：拾起时先失去生命值（走基类代价字段），再从牌库中升级若干张牌。</summary>
    [CreateAssetMenu(fileName = "Pomander", menuName = "DreamKeep/Data/Relic/橙型香盒")]
    public sealed class SO_Pomander : SO_Relic
    {
        [SerializeField] private int _cardCount = 2;

        public int CardCount => _cardCount;
    }
}
