using System;
using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.GameRun;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;

namespace _DreamKeep.GameRun
{
    /// <summary>商店段模型：进段时按场景固定的槽位数掷一次货架，之后回答报价、结算购买与删牌服务。
    /// 纯 C#，不认识引擎与容器；掷骰走整局唯一随机流，同种子可复现。
    /// 售出即把该格置空（列表保留下标），空位由视图隐藏、整行缩放交给 UI_RowScaleFitter。</summary>
    public sealed class ShopSession
    {
        /// <summary>普通+稀有卡槽位数：与场景 CardGroup_Common_Rare 的子项数一致。</summary>
        public const int CommonRareSlotCount = 4;

        /// <summary>超稀有卡槽位数：与场景 CardGroup_SuperRare 的子项数一致。</summary>
        public const int SuperRareSlotCount = 2;

        /// <summary>药水槽位数：与场景 PotionGroup 的子项数一致。</summary>
        public const int PotionSlotCount = 3;

        private readonly RunState _run;
        private readonly SO_ShopPrices _prices;

        private readonly List<CardModel> _commonRareCards = new();
        private readonly List<CardModel> _superRareCards = new();
        private readonly List<SO_Relic> _relics = new();
        private readonly List<SO_Potion> _potions = new();

        // 与各货架按下标对齐的进店浮动价：售出的格子随货架一起置 null 保下标
        private readonly List<int> _commonRarePrices = new();
        private readonly List<int> _superRarePrices = new();
        private readonly List<int> _relicPrices = new();
        private readonly List<int> _potionPrices = new();

        private int _commonRareSaleIndex = -1;
        private int _superRareSaleIndex = -1;
        private bool _removalUsed;

        /// <summary>普通+稀有卡货架：售出的格子为 null。</summary>
        public IReadOnlyList<CardModel> CommonRareCards => _commonRareCards;

        /// <summary>超稀有卡货架：售出的格子为 null。</summary>
        public IReadOnlyList<CardModel> SuperRareCards => _superRareCards;

        /// <summary>遗物货架（普通/稀有/商店各一件，缺货跳过）：售出的格子为 null。</summary>
        public IReadOnlyList<SO_Relic> Relics => _relics;

        /// <summary>药水货架：售出的格子为 null。</summary>
        public IReadOnlyList<SO_Potion> Potions => _potions;

        /// <summary>本次进店是否已用过删牌服务：每店限一次。</summary>
        public bool RemovalUsed => _removalUsed;

        /// <summary>当前金币：视图给价格染色时读，不直接碰整局状态。</summary>
        public int Gold => _run.Gold;

        /// <summary>删牌服务当前价：随整局已用次数递增，下次进店更贵。</summary>
        public int RemovalPrice => _prices.RemovalPrice(_run.CardRemovalsUsed);

        public ShopSession(RunState run_, SO_ShopPrices prices_, SO_CardPool normalCards_, SO_CardPool rareCards_,
            SO_CardPool superRareCards_, SO_RelicPool commonRelics_, SO_RelicPool rareRelics_,
            SO_RelicPool shopRelics_, SO_PotionPool commonPotions_, SO_PotionPool rarePotions_)
        {
            _run = run_;
            _prices = prices_;
            Rng rng = run_.Rng;

            RollCards(_commonRareCards, rng, CommonRareSlotCount, normalCards_, rareCards_);
            _commonRareSaleIndex = RollSaleIndex(rng, _commonRareCards.Count);
            RollPrices(_commonRarePrices, rng, _commonRareCards, card => _prices.CardPrice(card.Rarity, rng.Next(-1f, 1f)));
            RollCards(_superRareCards, rng, SuperRareSlotCount, superRareCards_);
            _superRareSaleIndex = RollSaleIndex(rng, _superRareCards.Count);
            RollPrices(_superRarePrices, rng, _superRareCards, card => _prices.CardPrice(card.Rarity, rng.Next(-1f, 1f)));
            RollRelics(rng, commonRelics_, rareRelics_, shopRelics_);
            RollPrices(_relicPrices, rng, _relics, relic => _prices.RelicPrice(relic.Rarity, rng.Next(-1f, 1f)));
            RollPotions(rng, commonPotions_, rarePotions_);
            RollPrices(_potionPrices, rng, _potions, potion => _prices.PotionPrice(potion.Rarity, rng.Next(-1f, 1f)));
        }

        #region 报价：视图只用这里拿数字，进店浮动价与半价不外泄换算

        /// <summary>该卡当前售价：半价卡取进店浮动价的一半（整除）；卡不在任一货架上时抛错。</summary>
        public int PriceOfCard(CardModel card_)
        {
            int index = _commonRareCards.IndexOf(card_);
            if (index >= 0) return SalePrice(_commonRarePrices[index], index == _commonRareSaleIndex);

            index = _superRareCards.IndexOf(card_);
            if (index < 0) throw new InvalidOperationException($"[{nameof(ShopSession)}] 请求报价的卡不在货架上");
            return SalePrice(_superRarePrices[index], index == _superRareSaleIndex);
        }

        /// <summary>该卡是否打了半价（两个卡牌组各自随机一张）。</summary>
        public bool IsOnSale(CardModel card_)
        {
            int index = _commonRareCards.IndexOf(card_);
            if (index >= 0) return index == _commonRareSaleIndex;
            return _superRareCards.IndexOf(card_) == _superRareSaleIndex;
        }

        /// <summary>该遗物的进店浮动价；不在货架上时抛错。</summary>
        public int PriceOfRelic(SO_Relic relic_)
        {
            int index = _relics.IndexOf(relic_);
            if (index < 0) throw new InvalidOperationException($"[{nameof(ShopSession)}] 请求报价的遗物不在货架上");
            return _relicPrices[index];
        }

        /// <summary>该药水的进店浮动价；不在货架上时抛错。</summary>
        public int PriceOfPotion(SO_Potion potion_)
        {
            int index = _potions.IndexOf(potion_);
            if (index < 0) throw new InvalidOperationException($"[{nameof(ShopSession)}] 请求报价的药水不在货架上");
            return _potionPrices[index];
        }

        #endregion

        #region 购买：先校验再发货，售出的格子置空

        /// <summary>买卡：扣款后把卡克隆成局内卡加入牌组（canonical 原型不进牌组）。</summary>
        public EShopBuyResult TryBuyCard(CardModel card_)
        {
            List<CardModel> shelf = card_ == null ? null : ShelfOfCard(card_);
            if (shelf == null) return EShopBuyResult.SoldOut;

            int price = PriceOfCard(card_);
            if (_run.Gold < price) return EShopBuyResult.NotEnoughGold;

            _run.SpendGold(price);
            _run.Player.AddCard(card_.ToMutable());
            shelf[shelf.IndexOf(card_)] = null;
            return EShopBuyResult.Success;
        }

        /// <summary>买遗物：扣款后入遗物栏；涟漪由段入口在成功后转播（模型不碰表现）。</summary>
        public EShopBuyResult TryBuyRelic(SO_Relic relic_)
        {
            int index = relic_ == null ? -1 : _relics.IndexOf(relic_);
            if (index < 0 || _run.Player.HasRelic(relic_)) return EShopBuyResult.SoldOut;

            int price = PriceOfRelic(relic_);
            if (_run.Gold < price) return EShopBuyResult.NotEnoughGold;

            _run.SpendGold(price);
            _run.Player.AddRelic(relic_);
            _relics[index] = null;
            return EShopBuyResult.Success;
        }

        /// <summary>买药水：先入槽再扣款——槽满时药水不落地、金币也不动。</summary>
        public EShopBuyResult TryBuyPotion(SO_Potion potion_)
        {
            int index = potion_ == null ? -1 : _potions.IndexOf(potion_);
            if (index < 0) return EShopBuyResult.SoldOut;

            int price = PriceOfPotion(potion_);
            if (_run.Gold < price) return EShopBuyResult.NotEnoughGold;
            if (!_run.Player.TryAddPotion(potion_)) return EShopBuyResult.PotionSlotsFull;

            _run.SpendGold(price);
            _potions[index] = null;
            return EShopBuyResult.Success;
        }

        #endregion

        #region 删牌服务：每店一次，价格随整局已用次数递增

        /// <summary>能否买删牌服务：本店没用过、且买得起。买不起也允许开面板会白选一轮，故一并挡在这里。</summary>
        public EShopBuyResult CheckRemoval()
        {
            if (_removalUsed) return EShopBuyResult.SoldOut;
            return _run.Gold < RemovalPrice ? EShopBuyResult.NotEnoughGold : EShopBuyResult.Success;
        }

        /// <summary>删牌成交：扣当前价、整局计数 +1、本店服务置灰。面板里的删牌由面板自己做。</summary>
        public void CompleteRemoval()
        {
            if (_removalUsed) return;
            _run.SpendGold(RemovalPrice);
            _run.RecordCardRemoval();
            _removalUsed = true;
        }

        #endregion

        // 卡所在货架：两个卡牌组各一份，找不到即已售出
        private List<CardModel> ShelfOfCard(CardModel card_)
        {
            if (_commonRareCards.Contains(card_)) return _commonRareCards;
            return _superRareCards.Contains(card_) ? _superRareCards : null;
        }

        // 每组随机一张半价：空组返回 -1（不消耗随机流）
        private static int RollSaleIndex(Rng rng_, int count_) => count_ == 0 ? -1 : rng_.Next(count_);

        // 每件货掷一次售价：偏移 [-1,1) 乘该类浮动幅度，进店即定死，整店期间不跳价
        private void RollPrices<T>(List<int> into_, Rng rng_, IReadOnlyList<T> items_, Func<T, int> priceOf_)
        {
            for (int i = 0; i < items_.Count; i++) into_.Add(priceOf_(items_[i]));
        }

        // 售价：半价卡取原价一半（整除）
        private static int SalePrice(int price_, bool onSale_) => onSale_ ? price_ / 2 : price_;

        // 卡：多池合并去重后不放回抽取，不足按实数出；canonical 原型先烘焙，卖出时才克隆成局内卡
        private void RollCards(List<CardModel> into_, Rng rng_, int count_, SO_CardPool first_, SO_CardPool second_ = null)
        {
            List<SO_Card> candidates = new();
            CollectCards(candidates, first_);
            CollectCards(candidates, second_);

            List<SO_Card> taken = rng_.TakeShuffled(candidates, count_);
            for (int i = 0; i < taken.Count; i++) into_.Add(CardModelBaker.Bake(taken[i]));
        }

        private static void CollectCards(List<SO_Card> into_, SO_CardPool pool_)
        {
            if (pool_ == null) return;
            IReadOnlyList<SO_Card> cards = pool_.Cards;
            for (int i = 0; i < cards.Count; i++)
            {
                SO_Card card = cards[i];
                if (card != null && !into_.Contains(card)) into_.Add(card);
            }
        }

        // 遗物：三池各出一件，跳过已持有与已上架；某池挑不出就空着该槽，不跨池补位
        private void RollRelics(Rng rng_, SO_RelicPool common_, SO_RelicPool rare_, SO_RelicPool shop_)
        {
            AddRelicFrom(rng_, common_);
            AddRelicFrom(rng_, rare_);
            AddRelicFrom(rng_, shop_);
        }

        private void AddRelicFrom(Rng rng_, SO_RelicPool pool_)
        {
            if (pool_ == null) return;

            // 附加过滤跳过已上架；等权取一件与逐个 Next 同消耗一格随机流
            List<SO_Relic> candidates = RelicRoller.CollectCandidates(_run.Player, new[] { pool_ },
                relic => !_relics.Contains(relic));
            if (candidates.Count == 0) return;
            _relics.Add(rng_.Pick(candidates));
        }

        // 药水：两池合并去重后不放回抽取三瓶
        private void RollPotions(Rng rng_, SO_PotionPool first_, SO_PotionPool second_)
        {
            List<SO_Potion> candidates = new();
            CollectPotions(candidates, first_);
            CollectPotions(candidates, second_);
            _potions.AddRange(rng_.TakeShuffled(candidates, PotionSlotCount));
        }

        private static void CollectPotions(List<SO_Potion> into_, SO_PotionPool pool_)
        {
            if (pool_ == null) return;
            IReadOnlyList<SO_Potion> potions = pool_.Potions;
            for (int i = 0; i < potions.Count; i++)
            {
                SO_Potion potion = potions[i];
                if (potion != null && !into_.Contains(potion)) into_.Add(potion);
            }
        }
    }
}
