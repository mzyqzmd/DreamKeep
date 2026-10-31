using System;
using System.Collections.Generic;
using _DreamKeep.RuntimeData;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity; // InjectGameObject 扩展方法住在这里

namespace _DreamKeep.CommonPresentation
{
    /// <summary>卡牌三选一面板：外界给若干张牌与"选中一张""跳过"两个回调，面板铺出对应数量的牌面并收集点击。
    /// 左键点牌=选中、右键点牌=打开统一卡牌详情特写（纯展示：点特写背板关闭，展示口径归特写自己的开关行）——
    /// 点牌与跳过都只对外回报、不关自己——牌是否进牌库、面板何时收起，全由调用方决定；不传跳过回调时跳过按钮不显示。</summary>
    public sealed class UI_CardOneInThreePanel : MonoBehaviour
    {
        [SerializeField] private RectTransform _cardsGroup;
        [SerializeField] private UI_CardSelect _cardPrefab;
        [SerializeField] private Button _skipButton;

        [Header("详情特写")] [Tooltip("统一卡牌详情特写预制体：右键陈列卡打开，实例化在本面板名下")] [SerializeField]
        private UI_ConcreteCardView _concrete; // 懒实例化的特写层：面板关时一并收起

        private readonly List<UI_CardSelect> _cards = new(); // 当前展示的牌实例（关闭时全部销毁）
        private IObjectResolver _resolver;
        private Action<CardModel> _onSelected;
        private Action _onSkipped;

        private void Awake() => _skipButton.onClick.AddListener(OnSkipClicked);

        private void OnDestroy() => _skipButton.onClick.RemoveListener(OnSkipClicked);

        // 外部失活兜底：特写收起，不留挂着的详情层
        private void OnDisable()
        {
            if (_concrete) _concrete.Close();
        }

        [Inject]
        public void Construct(IObjectResolver resolver_) => _resolver = resolver_;

        #region 开合与内容：外界给牌与两个回调，面板只负责铺牌与回报

        /// <summary>用给定牌铺开列表；<paramref name="onSkipped_"/> 为 null 时不显示跳过按钮。</summary>
        public void Show(IReadOnlyList<CardModel> cards_, Action<CardModel> onSelected_, Action onSkipped_)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling(); // 面板由工厂懒建，取出时要压住下层奖励面板
            _onSelected = onSelected_;
            _onSkipped = onSkipped_;
            _skipButton.gameObject.SetActive(onSkipped_ != null);
            ClearCards();
            for (int i = 0; i < cards_.Count; i++)
            {
                UI_CardSelect card = Instantiate(_cardPrefab, _cardsGroup);
                // 池外生成的实例要补注入：牌上的悬停气泡只从容器取宿主（PopFactory），漏注入会静默不弹
                _resolver.InjectGameObject(card.gameObject);
                card.Bind(cards_[i]);
                card.Clicked += OnCardClicked;
                card.RightClicked += OnCardRightClicked;
                _cards.Add(card);
            }
        }

        /// <summary>收起面板：牌实例全部销毁，下次 Show 重新生成（面板实例本身留给工厂复用）。</summary>
        public void Close()
        {
            ClearCards();
            _onSelected = null;
            _onSkipped = null;
            if (_concrete) _concrete.Close();
            gameObject.SetActive(false);
        }

        #endregion

        #region 详情特写：右键陈列卡打开统一特写

        // 右键陈列卡：把真卡交给统一特写（展示口径归它的开关行）
        private void OnCardRightClicked(UI_CardSelect card_) => _concrete.Open(card_.Card);

        #endregion

        private void OnCardClicked(UI_CardSelect card_) => _onSelected?.Invoke(card_.Card);

        private void OnSkipClicked() => _onSkipped?.Invoke();

        private void ClearCards()
        {
            foreach (UI_CardSelect card in _cards)
            {
                if (!card) continue;
                card.Clicked -= OnCardClicked;
                card.RightClicked -= OnCardRightClicked;
                card.gameObject.SetActive(false); // 销毁要到帧末生效：先失活免得同帧重开时旧牌还排在新牌旁边
                Destroy(card.gameObject);
            }

            _cards.Clear();
        }
    }
}