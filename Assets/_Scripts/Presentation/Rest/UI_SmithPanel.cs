using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;
using _DreamKeep.CommonPresentation;
using _DreamKeep.EngineUtility;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Presentation
{
    /// <summary>锻造弹板：先只列卡组中可升级的卡；左键选卡才打开原卡/升级预览（返回键回列表），
    /// 右键选卡打开统一卡牌详情特写（UI_ConcreteCardView，可切查看升级/基础版，展示口径归特写自己的开关行）；
    /// 列表面板的关闭键才是取消锻造。确认即升级并交还。开合能力继承自 UI_Panel，列表构建落在 OnShown。</summary>
    public sealed class UI_SmithPanel : UI_Panel
    {
        [Header("列表")] [SerializeField] private Transform _scrollContent;
        [SerializeField] private UI_CardInDeck _cardPrefab;

        [Header("升级预览")] [SerializeField] private UI_CardInDeck _cardUnupgrade;
        [SerializeField] private UI_CardInDeck _cardUpgraded;
        [SerializeField] private Button _previewBackButton;
        [SerializeField] private Button _confirmButton;

        [Header("右键详情")] [Tooltip("统一卡牌详情特写预制体：右键选卡打开，实例化在本面板名下")] [SerializeField]
        private UI_ConcreteCardView _concrete; // 懒实例化的特写层：面板关时一并收起

        [Header("开合")] [SerializeField] private Button _closeButton;

        private IObjectResolver _resolver;
        private MonoObjectPool<UI_CardInDeck> _pool;
        private readonly List<CardModel> _upgradable = new();
        private readonly List<UI_CardInDeck> _activeItems = new();
        private CardModel _selected;
        private GameObject _previewRoot;

        private Player _player;
        private UniTaskCompletionSource<CardModel> _result;

        private void Awake()
        {
            _previewRoot = _cardUnupgrade ? _cardUnupgrade.transform.parent.gameObject : null;
            _confirmButton.onClick.AddListener(OnConfirm);
            _closeButton.onClick.AddListener(OnPanelClose);
            if (_previewBackButton) _previewBackButton.onClick.AddListener(OnPreviewBack);
        }

        private void OnDestroy()
        {
            _confirmButton.onClick.RemoveListener(OnConfirm);
            _closeButton.onClick.RemoveListener(OnPanelClose);
            if (_previewBackButton) _previewBackButton.onClick.RemoveListener(OnPreviewBack);
        }

        // 外部失活兜底：两层覆盖与列表实例一并收掉，池实例不滞留
        private void OnDisable()
        {
            if (_previewRoot) _previewRoot.SetActive(false);
            if (_concrete) _concrete.Close();
            ReleaseItems();
        }

        public void Initialize(IObjectResolver resolver_)
        {
            _resolver = resolver_;
            _pool = new MonoObjectPool<UI_CardInDeck>(_scrollContent, 64,
                onCreate_: instance_ => _resolver.InjectGameObject(instance_.gameObject));
        }

        #region 对外：展示可升级列表并等待玩家决定

        public void Show(Player player_)
        {
            _player = player_;
            Show(); // 交由 UI_Panel.Show 触发 OnShown 构建列表
        }

        public async UniTask<CardModel> WaitResultAsync()
        {
            _result = new UniTaskCompletionSource<CardModel>();
            return await _result.Task;
        }

        #endregion

        protected override void OnShown()
        {
            _selected = null;
            if (_previewRoot) _previewRoot.SetActive(false);
            if (_concrete) _concrete.Close();
            ReleaseItems();

            _upgradable.Clear();
            foreach (CardModel card in _player.Deck.Cards)
                if (card.CanUpgrade)
                    _upgradable.Add(card);

            for (int i = 0; i < _upgradable.Count; i++)
            {
                CardModel card = _upgradable[i];
                UI_CardInDeck item = _pool.Get(_cardPrefab);
                item.transform.SetSiblingIndex(i);
                item.Bind(card, _ => SelectCard(card), _ => _concrete.Open(card));
                _activeItems.Add(item);
            }
        }

        // 左键选卡：打开原卡/升级预览层（列表仍在背后）
        private void SelectCard(CardModel card_)
        {
            _selected = card_;
            if (_previewRoot) _previewRoot.SetActive(true);
            _cardUnupgrade.gameObject.SetActive(true);
            _cardUpgraded.gameObject.SetActive(true);
            _cardUnupgrade.Bind(card_, null);
            CardModel preview = card_.ToMutable();
            preview.Upgrade();
            _cardUpgraded.Bind(preview, null);
        }

        // 预览返回：只关预览层回到列表，不算取消锻造
        private void OnPreviewBack()
        {
            if (_previewRoot) _previewRoot.SetActive(false);
        }

        // 列表面板关闭：取消锻造，交还 null 由入口回到选项
        private void OnPanelClose() => _result?.TrySetResult(null);

        private void OnConfirm()
        {
            if (_selected == null) return;
            // 先取走选中态再关板：同帧手快再敲确认时 _selected 已空，不会把第二张也升了
            CardModel card = _selected;
            _selected = null;
            card.Upgrade();
            Hide(); // 确认即整板收起：预览与列表一起消失，防止玩家连敲多张牌
            _result?.TrySetResult(card);
        }

        private void ReleaseItems()
        {
            if (_pool == null) return;
            for (int i = _activeItems.Count - 1; i >= 0; i--)
                if (_activeItems[i])
                    _pool.Release(_activeItems[i]);
            _activeItems.Clear();
        }
    }
}