using System;
using System.Collections.Generic;
using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>商店货架快照：事件负载专用的只读陈列数据，复制货架与价格表，不持有会话与场景引用。
    /// 价格表即实付价（两个卡货架已折入半价），查询面与 ShopSession 的只读部分一致；IsOnSale 只作视图折扣标识。
    /// 金币不随快照，染色一律实时取 RunState.Gold，避免快照与整局状态两个真源。</summary>
    public sealed class ShopShelfSnapshot
    {
        private readonly List<int> _commonRarePrices;
        private readonly List<int> _superRarePrices;
        private readonly List<int> _relicPrices;
        private readonly List<int> _potionPrices;
        private readonly List<bool> _commonRareOnSale;
        private readonly List<bool> _superRareOnSale;

        public IReadOnlyList<CardModel> CommonRareCards { get; }
        public IReadOnlyList<CardModel> SuperRareCards { get; }
        public IReadOnlyList<SO_Relic> Relics { get; }
        public IReadOnlyList<SO_Potion> Potions { get; }
        public int RemovalPrice { get; }
        public bool RemovalUsed { get; }

        public ShopShelfSnapshot(IReadOnlyList<CardModel> commonRareCards_, IReadOnlyList<CardModel> superRareCards_,
            IReadOnlyList<SO_Relic> relics_, IReadOnlyList<SO_Potion> potions_, List<int> commonRarePrices_,
            List<int> superRarePrices_, List<int> relicPrices_, List<int> potionPrices_,
            List<bool> commonRareOnSale_, List<bool> superRareOnSale_, int removalPrice_, bool removalUsed_)
        {
            CommonRareCards = new List<CardModel>(commonRareCards_);
            SuperRareCards = new List<CardModel>(superRareCards_);
            Relics = new List<SO_Relic>(relics_);
            Potions = new List<SO_Potion>(potions_);
            _commonRarePrices = commonRarePrices_;
            _superRarePrices = superRarePrices_;
            _relicPrices = relicPrices_;
            _potionPrices = potionPrices_;
            _commonRareOnSale = commonRareOnSale_;
            _superRareOnSale = superRareOnSale_;
            RemovalPrice = removalPrice_;
            RemovalUsed = removalUsed_;
        }

        public int PriceOfCard(CardModel card_)
        {
            for (int i = 0; i < CommonRareCards.Count; i++)
                if (CommonRareCards[i] == card_) return _commonRarePrices[i];
            for (int i = 0; i < SuperRareCards.Count; i++)
                if (SuperRareCards[i] == card_) return _superRarePrices[i];
            throw new InvalidOperationException($"[{nameof(ShopShelfSnapshot)}] 请求报价的卡不在货架上");
        }

        public bool IsOnSale(CardModel card_)
        {
            for (int i = 0; i < CommonRareCards.Count; i++)
                if (CommonRareCards[i] == card_) return _commonRareOnSale[i];
            for (int i = 0; i < SuperRareCards.Count; i++)
                if (SuperRareCards[i] == card_) return _superRareOnSale[i];
            return false;
        }

        public int PriceOfRelic(SO_Relic relic_)
        {
            for (int i = 0; i < Relics.Count; i++)
                if (Relics[i] == relic_) return _relicPrices[i];
            throw new InvalidOperationException($"[{nameof(ShopShelfSnapshot)}] 请求报价的遗物不在货架上");
        }

        public int PriceOfPotion(SO_Potion potion_)
        {
            for (int i = 0; i < Potions.Count; i++)
                if (Potions[i] == potion_) return _potionPrices[i];
            throw new InvalidOperationException($"[{nameof(ShopShelfSnapshot)}] 请求报价的药水不在货架上");
        }
    }
}
