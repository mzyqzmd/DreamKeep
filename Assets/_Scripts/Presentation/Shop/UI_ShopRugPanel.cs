using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using _DreamKeep.CommonPresentation;
using _DreamKeep.ConfigData;
using _DreamKeep.EngineUtility;
using _DreamKeep.GameRun;
using _DreamKeep.RuntimeData;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>商店货摊（地毯）：货架的全部视图、买卖交互与开合动画都在这里。
    /// 槽位固定用场景里摆好的实例，不池化不实例化——四个分组按父容器收集子项，绑定时逐格填；
    /// 售出的格子保留根节点占位（只收内容），整行位置因此不会重排。
    /// 陈列数据一律由货架快照事实带出，点击只提交命令；右键特写与商人手交接仍归本类。</summary>
    public sealed class UI_ShopRugPanel : UI_Panel
    {
        [Header("货架分组")]
        [Tooltip("普通+稀有卡组（4 格）")] [SerializeField]
        private RectTransform _commonRareGroup;

        [Tooltip("超稀有卡组（2 格）")] [SerializeField]
        private RectTransform _superRareGroup;

        [Tooltip("遗物组（3 格）")] [SerializeField]
        private RectTransform _relicGroup;

        [Tooltip("药水组（3 格）")] [SerializeField]
        private RectTransform _potionGroup;

        [Header("其它交互件")]
        [Tooltip("删牌服务位")] [SerializeField]
        private UI_RemoveCardButton _removeCardButton;

        [Tooltip("关闭按钮：收起货摊，可再点商人掀开")] [SerializeField]
        private Button _closeButton;

        [Tooltip("商人手：悬停商品时指过去")] [SerializeField]
        private UI_MerchantHand _merchantHand;

        [Tooltip("统一卡牌特写：右键卡位打开（在 Canvas_HUD 下，打开时压住货摊）")] [SerializeField]
        private UI_ConcreteCardView _concreteView;

        [Header("开合")]
        [Tooltip("打开时从本位上方多少画布像素滑下来")] [SerializeField]
        private float _openDropPx = 300f;

        [Tooltip("打开滑动时长（秒）")] [SerializeField] private float _openDuration = 0.3f;

        [Tooltip("关闭滑动时长（秒）")] [SerializeField] private float _closeDuration = 0.25f;

        private readonly List<UI_CardShop> _commonRareCards = new();
        private readonly List<UI_CardShop> _superRareCards = new();
        private readonly List<UI_RelicShopItem> _relics = new();
        private readonly List<UI_PotionShopItem> _potions = new();

        private ShopShelfSnapshot _shelf;
        private ShopCommandDispatcher _commands;
        private UI_ShopItem _pendingItem; // 被拒反馈只针对刚点的那一格：命令同步结算，事件同步回来
        private Component _hovered; // 商品与删牌服务共用：同一时刻只有一个悬停来源
        private Tween _slideTween;
        private bool _isClosing;
        private Vector2 _basePosition;
        private bool _baseCaptured;
        private bool _wired;
        private PopPanelFactory _popFactory;

        private void Awake()
        {
            EnsureWired();
            BasePosition();
        }

        private void OnDisable() => _slideTween?.Kill();

        protected override void OnHidden()
        {
            _slideTween?.Kill();
            _isClosing = false;
            CloseSubLayers(false);
        }

        [Inject]
        public void Construct(PopPanelFactory popFactory_, ShopEventDispatcher events_, ShopCommandDispatcher commands_)
        {
            // 段入口会为地毯子树再补注入一次：订阅只做一次，否则同一事实会播出多遍
            if (_commands != null) return;
            _popFactory = popFactory_;
            _commands = commands_;
            events_.Register<SE_ShelfUpdated>(OnShelfUpdated);
            events_.Register<SE_ShopOpened>(opened_ => PlayOpenAsync().Forget());
            events_.Register<SE_PurchaseRefused>(refused_ => OnPurchaseRefused());
            events_.Register<SE_RemovalRefused>(refused_ => PlayRemovalRefuse());
        }

        private void OnShelfUpdated(SE_ShelfUpdated updated_)
        {
            _shelf = updated_.Shelf;
            if (updated_.Initial) Bind();
            else RefreshPrices();
        }

        #region 对外：开合、重染价格与被拒反馈

        /// <summary>掀开货摊：从本位上方滑下来，商人手同步落下；滑动结束才返回。</summary>
        public async UniTask PlayOpenAsync()
        {
            EnsureWired();
            gameObject.SetActive(true); // 保险：激活会补跑 Awake（首次打开时 EnsureWired 已在上面完成）
            _isClosing = false;

            RectTransform rect = (RectTransform)transform;
            Vector2 basePosition = BasePosition();
            _slideTween?.Kill();
            rect.anchoredPosition = basePosition + new Vector2(0f, _openDropPx);
            if (_merchantHand) _merchantHand.PlayEnter();

            _slideTween = rect.DOAnchorPos(basePosition, _openDuration).SetEase(Ease.OutCubic);
            await _slideTween.WaitAsync();
        }

        /// <summary>收起货摊：向上滑回上方后隐藏；特写当场收起，商人手跟着面板一起滑出去再隐藏。</summary>
        public void PlayClose()
        {
            EnsureWired();
            RectTransform rect = (RectTransform)transform;
            Vector2 basePosition = BasePosition();
            _slideTween?.Kill();
            _isClosing = true; // 关闭期间面板仍激活：悬停一律不生效，免得把刚退场的手重新唤醒
            CloseSubLayers(true);

            _slideTween = rect.DOAnchorPos(basePosition + new Vector2(0f, _openDropPx), _closeDuration)
                .SetEase(Ease.InCubic)
                .OnComplete(() =>
                {
                    rect.anchoredPosition = basePosition;
                    gameObject.SetActive(false);
                    if (_merchantHand) _merchantHand.HideImmediate(); // 兜底：退场补间被打断也要收干净
                    _isClosing = false;
                });
        }

        /// <summary>删牌服务被拒（金币不足）：抖动提示，不弹板。</summary>
        public void PlayRemovalRefuse()
        {
            if (_removeCardButton) _removeCardButton.PlayRefuse();
        }

        /// <summary>删牌面板按下确认的那一刻：让删牌服务位播一次翻面并进入不可用态。</summary>
        public void PlayRemovalFlip()
        {
            if (_removeCardButton) _removeCardButton.PlayFlip();
        }

        #endregion

        // 首次快照：逐格填内容与价格，缺货的格子留位收起
        private void Bind()
        {
            EnsureWired();
            BindCards(_commonRareCards, _shelf.CommonRareCards);
            BindCards(_superRareCards, _shelf.SuperRareCards);
            BindRelics();
            BindPotions();
            RefreshPrices();
        }

        /// <summary>按当前快照重染整行价格：每次成交与删牌后调用一次（最多 12 格，不做每帧轮询）。</summary>
        private void RefreshPrices()
        {
            if (_shelf == null) return;

            int gold = _shelf.Gold;
            RefreshCards(_commonRareCards, _shelf.CommonRareCards, gold);
            RefreshCards(_superRareCards, _shelf.SuperRareCards, gold);
            RefreshRelics(gold);
            RefreshPotions(gold);
            if (_removeCardButton) _removeCardButton.ShowPrice(_shelf.RemovalPrice, gold, _shelf.RemovalUsed);
        }

        private void BindCards(List<UI_CardShop> views_, IReadOnlyList<CardModel> shelf_)
        {
            for (int i = 0; i < views_.Count; i++)
            {
                CardModel card = i < shelf_.Count ? shelf_[i] : null;
                UI_CardShop view = views_[i];
                if (card == null)
                {
                    view.MarkSold();
                    continue;
                }

                view.Bind(card, _shelf.PriceOfCard(card), _shelf.IsOnSale(card), _shelf.Gold);
            }
        }

        private void BindRelics()
        {
            IReadOnlyList<SO_Relic> shelf = _shelf.Relics;
            for (int i = 0; i < _relics.Count; i++)
            {
                SO_Relic relic = i < shelf.Count ? shelf[i] : null;
                UI_RelicShopItem view = _relics[i];
                if (relic == null)
                {
                    view.MarkSold();
                    continue;
                }

                view.Bind(relic, _shelf.PriceOfRelic(relic), _shelf.Gold);
            }
        }

        private void BindPotions()
        {
            IReadOnlyList<SO_Potion> shelf = _shelf.Potions;
            for (int i = 0; i < _potions.Count; i++)
            {
                SO_Potion potion = i < shelf.Count ? shelf[i] : null;
                UI_PotionShopItem view = _potions[i];
                if (potion == null)
                {
                    view.MarkSold();
                    continue;
                }

                view.Bind(potion, _shelf.PriceOfPotion(potion), _shelf.Gold);
            }
        }

        private void RefreshCards(List<UI_CardShop> views_, IReadOnlyList<CardModel> shelf_, int gold_)
        {
            for (int i = 0; i < views_.Count; i++)
            {
                CardModel card = i < shelf_.Count ? shelf_[i] : null;
                if (card == null)
                {
                    views_[i].MarkSold();
                    continue;
                }

                views_[i].ShowPrice(_shelf.PriceOfCard(card), gold_, _shelf.IsOnSale(card));
            }
        }

        private void RefreshRelics(int gold_)
        {
            IReadOnlyList<SO_Relic> shelf = _shelf.Relics;
            for (int i = 0; i < _relics.Count; i++)
            {
                SO_Relic relic = i < shelf.Count ? shelf[i] : null;
                if (relic == null)
                {
                    _relics[i].MarkSold();
                    continue;
                }

                _relics[i].ShowPrice(_shelf.PriceOfRelic(relic), gold_);
            }
        }

        private void RefreshPotions(int gold_)
        {
            IReadOnlyList<SO_Potion> shelf = _shelf.Potions;
            for (int i = 0; i < _potions.Count; i++)
            {
                SO_Potion potion = i < shelf.Count ? shelf[i] : null;
                if (potion == null)
                {
                    _potions[i].MarkSold();
                    continue;
                }

                _potions[i].ShowPrice(_shelf.PriceOfPotion(potion), gold_);
            }
        }

        // 左键：只上报购买意图，结算与结果反馈都交回域层
        private void OnBuyRequested(UI_ShopItem item_)
        {
            _pendingItem = item_;
            switch (item_)
            {
                case UI_CardShop cardView:
                    _commands.BuyCard(cardView.Card);
                    break;
                case UI_RelicShopItem relicView:
                    _commands.BuyRelic(relicView.Relic);
                    break;
                case UI_PotionShopItem potionView:
                    _commands.BuyPotion(potionView.Potion);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(item_), $"{item_.GetType().Name} 不是商店商品");
            }
        }

        private void OnPurchaseRefused()
        {
            if (_pendingItem) _pendingItem.PlayRefuse();
            _pendingItem = null;
        }

        // 右键：卡位交给统一特写，遗物格交给工厂统一的遗物信息面板
        private void OnDetailRequested(UI_ShopItem item_)
        {
            if (item_ is UI_CardShop cardView && _concreteView) _concreteView.Open(cardView.Card);
            if (item_ is UI_RelicShopItem relicView) _popFactory.AcquireRelicInfoPanel().Show(relicView.Relic, false);
        }

        // 悬停交接：Exit 晚于下一格的 Enter 到达时不清掉新的悬停；商品与删牌服务共用这一条通道
        private void OnHovered(Component source_)
        {
            if (_isClosing) return;
            _hovered = source_;
            if (_merchantHand) _merchantHand.PointAt(source_.transform);
        }

        private void OnUnhovered(Component source_)
        {
            if (_hovered != source_) return;
            _hovered = null;
            if (_merchantHand) _merchantHand.ResetPoint();
        }

        private void OnRemoveClicked() => _commands.RequestRemoval();

        // 特写与商人手在关摊时一并收起：动画关闭让手跟着面板滑出去，立即关闭（进店/兜底）直接收
        private void CloseSubLayers(bool animateHand_)
        {
            if (_concreteView) _concreteView.Close();
            if (_merchantHand)
            {
                if (animateHand_) _merchantHand.PlayExit(_openDropPx, _closeDuration);
                else _merchantHand.HideImmediate();
            }

            _hovered = null;
        }

        // 本位只取一次：之后所有开合都相对它算，场景里摆放的值就是最终位置
        private Vector2 BasePosition()
        {
            if (_baseCaptured) return _basePosition;
            _baseCaptured = true;
            _basePosition = ((RectTransform)transform).anchoredPosition;
            return _basePosition;
        }

        // 引用接线只做一次：分组子项收集 + 按钮与商品事件的统一挂钩
        private void EnsureWired()
        {
            if (_wired) return;
            _wired = true;

            if (_commonRareGroup) _commonRareGroup.GetComponentsInChildren(true, _commonRareCards);
            if (_superRareGroup) _superRareGroup.GetComponentsInChildren(true, _superRareCards);
            if (_relicGroup) _relicGroup.GetComponentsInChildren(true, _relics);
            if (_potionGroup) _potionGroup.GetComponentsInChildren(true, _potions);

            if (_closeButton) _closeButton.onClick.AddListener(PlayClose);
            if (_removeCardButton)
            {
                _removeCardButton.Clicked += OnRemoveClicked;
                _removeCardButton.Hovered += OnHovered;
                _removeCardButton.Unhovered += OnUnhovered;
            }

            for (int i = 0; i < _commonRareCards.Count; i++) HookItem(_commonRareCards[i]);
            for (int i = 0; i < _superRareCards.Count; i++) HookItem(_superRareCards[i]);
            for (int i = 0; i < _relics.Count; i++) HookItem(_relics[i]);
            for (int i = 0; i < _potions.Count; i++) HookItem(_potions[i]);
        }

        private void HookItem(UI_ShopItem item_)
        {
            item_.BuyRequested += OnBuyRequested;
            item_.DetailRequested += OnDetailRequested;
            item_.Hovered += OnHovered;
            item_.Unhovered += OnUnhovered;
        }
    }
}
