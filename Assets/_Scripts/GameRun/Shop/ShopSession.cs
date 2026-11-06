using System;
using System.Collections.Generic;
using _DreamKeep.ConfigData;
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
            if (index < 0) return EShopBuyResult.SoldOut;

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

        #region 货架快照：表现层铺格子与重染价格的唯一数据源

        /// <summary>货架快照：售出格为 null（价格记 0、半价记 false），视图按 null 收起该格。
        /// 组装直接用内部名册与价格表，不经公开报价面反查重组。</summary>
        public ShopShelfSnapshot BuildSnapshot()
        {
            return new ShopShelfSnapshot(_commonRareCards, _superRareCards, _relics, _potions,
                PriceSnapshot(_commonRareCards, _commonRarePrices), PriceSnapshot(_superRareCards, _superRarePrices),
                PriceSnapshot(_relics, _relicPrices), PriceSnapshot(_potions, _potionPrices),
                SaleSnapshot(_commonRareCards, _commonRareSaleIndex), SaleSnapshot(_superRareCards, _superRareSaleIndex),
                _run.Gold, RemovalPrice, _removalUsed);
        }

        // 价格快照：售出格价格记 0
        private static List<int> PriceSnapshot<T>(List<T> shelf_, List<int> prices_) where T : class
        {
            List<int> snapshot = new(shelf_.Count);
            for (int i = 0; i < shelf_.Count; i++) snapshot.Add(shelf_[i] == null ? 0 : prices_[i]);
            return snapshot;
        }

        // 半价快照：售出格记 false
        private static List<bool> SaleSnapshot(List<CardModel> shelf_, int saleIndex_)
        {
            List<bool> snapshot = new(shelf_.Count);
            for (int i = 0; i < shelf_.Count; i++) snapshot.Add(shelf_[i] != null && i == saleIndex_);
            return snapshot;
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

        // 卡：多池合并按权重去重后不放回抽取，不足按实数出；canonical 原型先烘焙，卖出时才克隆成局内卡
        private void RollCards(List<CardModel> into_, Rng rng_, int count_, SO_CardPool first_, SO_CardPool second_ = null)
        {
            List<WeightedItem<SO_Card>> candidates = EncounterRewardRoller.CollectWeighted(
                new[] { first_, second_ }, static pool_ => pool_.Cards, static pool_ => pool_.Weight);

            List<SO_Card> taken = rng_.TakeWeighted(candidates, count_);
            for (int i = 0; i < taken.Count; i++) into_.Add(CardModelBaker.Bake(taken[i]));
        }

        // 遗物：三池各出一件，附加过滤跳过已上架；挑不出时由 RelicRoller 降级到兜底池补位
        private void RollRelics(Rng rng_, SO_RelicPool common_, SO_RelicPool rare_, SO_RelicPool shop_)
        {
            AddRelicFrom(rng_, common_);
            AddRelicFrom(rng_, rare_);
            AddRelicFrom(rng_, shop_);
        }

        private void AddRelicFrom(Rng rng_, SO_RelicPool pool_)
        {
            if (pool_ == null) return;

            // 附加过滤只作用于常规池；兜底池补齐由 RelicRoller 内部完成，不受"跳过已上架"影响
            SO_Relic picked = RelicRoller.RollOne(_run, new[] { pool_ }, rng_,
                relic => !_relics.Contains(relic));
            if (picked == null) return; // 兜底未接线时留空槽
            _relics.Add(picked);
        }

        // 药水：两池合并按权重去重后不放回抽取三瓶
        private void RollPotions(Rng rng_, SO_PotionPool first_, SO_PotionPool second_)
        {
            List<WeightedItem<SO_Potion>> candidates = EncounterRewardRoller.CollectWeighted(
                new[] { first_, second_ }, static pool_ => pool_.Potions, static pool_ => pool_.Weight);
            _potions.AddRange(rng_.TakeWeighted(candidates, PotionSlotCount));
        }
    }
}
