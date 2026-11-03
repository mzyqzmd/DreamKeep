using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>商店价目表：各档位基础价、删牌涨价步进与三类商品的浮动幅度，是商店段所有报价的唯一配置真源。
    /// 浮动偏移量由商店段模型掷出后传入，本资产只回答"某档位在某个偏移下多少钱"；半价（取整除二）与涨价时机由商店段模型换算。</summary>
    [CreateAssetMenu(fileName = "NewPrices", menuName = "DreamKeep/Data/ShopPrices")]
    public class SO_ShopPrices : ScriptableObject
    {
        [Header("卡牌")] [Tooltip("普通卡价格")] [SerializeField]
        private int _cardNormal;

        [Tooltip("稀有卡价格")] [SerializeField] private int _cardRare;
        [Tooltip("超稀有卡价格")] [SerializeField] private int _cardSuperRare;

        [Tooltip("卡牌价格浮动幅度：0.15 即每张卡在基准价 ±15% 内随机浮动")] [SerializeField]
        private float _cardFluctuation;

        [Header("遗物")] [Tooltip("普通遗物价格")] [SerializeField]
        private int _relicCommon;

        [Tooltip("稀有遗物价格")] [SerializeField] private int _relicRare;
        [Tooltip("商店遗物价格")] [SerializeField] private int _relicShop;

        [Tooltip("遗物价格浮动幅度：0.15 即每件遗物在基准价 ±15% 内随机浮动")] [SerializeField]
        private float _relicFluctuation;

        [Header("药水")] [Tooltip("普通药水价格")] [SerializeField]
        private int _potionCommon;

        [Tooltip("稀有药水价格")] [SerializeField] private int _potionRare;

        [Tooltip("药水价格浮动幅度：0.15 即每瓶药水在基准价 ±15% 内随机浮动")] [SerializeField]
        private float _potionFluctuation;

        [Header("删牌服务")] [Tooltip("首次价格")] [SerializeField]
        private int _removalBase;

        [Tooltip("每用一次的涨价：价格 = 首次价格 + 该值 × 整局已用次数")] [SerializeField]
        private int _removalStep;

        #region 报价：档位与浮动偏移到价格，未登记的档位直接抛错

        // roll_ 为 [-1,1) 的偏移量，由商店段模型掷出；浮动幅度为 0 时退化为基准价

        public int CardPrice(ECardRarity rarity_, float roll_) => rarity_ switch
        {
            ECardRarity.Normal => Mathf.CeilToInt(_cardNormal * (1f + roll_ * _cardFluctuation)),
            ECardRarity.Rare => Mathf.CeilToInt(_cardRare * (1f + roll_ * _cardFluctuation)),
            ECardRarity.SuperRare => Mathf.CeilToInt(_cardSuperRare * (1f + roll_ * _cardFluctuation)),
            _ => throw new ArgumentOutOfRangeException(nameof(rarity_), $"{rarity_} 没有登记商店价格"),
        };

        public int RelicPrice(ERelicRarity rarity_, float roll_) => rarity_ switch
        {
            ERelicRarity.Common => Mathf.CeilToInt(_relicCommon * (1f + roll_ * _relicFluctuation)),
            ERelicRarity.Rare => Mathf.CeilToInt(_relicRare * (1f + roll_ * _relicFluctuation)),
            ERelicRarity.Shop => Mathf.CeilToInt(_relicShop * (1f + roll_ * _relicFluctuation)),
            _ => throw new ArgumentOutOfRangeException(nameof(rarity_), $"{rarity_} 不进商店，没有登记价格"),
        };

        public int PotionPrice(EPotionRarity rarity_, float roll_) => rarity_ switch
        {
            EPotionRarity.Common => Mathf.CeilToInt(_potionCommon * (1f + roll_ * _potionFluctuation)),
            EPotionRarity.Rare => Mathf.CeilToInt(_potionRare * (1f + roll_ * _potionFluctuation)),
            _ => throw new ArgumentOutOfRangeException(nameof(rarity_), $"{rarity_} 没有登记商店价格"),
        };

        /// <summary>删牌服务当前价：首次价 + 步进 × 已用次数，越用越贵。</summary>
        public int RemovalPrice(int usedCount_) => _removalBase + _removalStep * usedCount_;

        #endregion
    }
}