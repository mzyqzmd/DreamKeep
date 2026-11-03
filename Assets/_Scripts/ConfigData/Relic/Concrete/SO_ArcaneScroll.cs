using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>奥术卷轴：拾起时从配置的金卡池里随机取一张加入牌库，无持续效果。</summary>
    [CreateAssetMenu(fileName = "ArcaneScroll", menuName = "DreamKeep/Data/Relic/奥术卷轴")]
    public sealed class SO_ArcaneScroll : SO_Relic
    {
        [Tooltip("候选卡池：指向最高稀有度的卡池资产（CardPool_SuperRare）")]
        [SerializeField]
        private SO_CardPool _cardPool;

        public SO_CardPool CardPool => _cardPool;
    }
}
