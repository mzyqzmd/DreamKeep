using System;
using System.Collections.Generic;
using _DreamKeep.CommonPresentation;
using _DreamKeep.EngineUtility;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

namespace _DreamKeep.Presentation
{
    /// <summary>战斗内手牌选择面板：升级与消耗两模式互斥显隐，选中的原卡离手后由本面板的陈列卡接管显示与点击，
    /// 升级模式额外陈列升级后副本；面板只把选中列表交回域侧，不写任何域状态。</summary>
    public sealed class UI_BattleSelectCardPanel : UI_Panel
    {
        [Header("分区")] [SerializeField] private GameObject _background;
        [SerializeField] private GameObject _selectSection;
        [SerializeField] private GameObject _upgradeSection;
        [Tooltip("提示文本：升级/消耗两套文案按请求模式刷新，数字取目标张数")] [SerializeField]
        private TextMeshProUGUI _tipText;

        [Header("陈列")] [SerializeField] private RectTransform _selectedGroup;
        [SerializeField] private RectTransform _unupgradeGroup;
        [SerializeField] private RectTransform _upgradedGroup;
        [SerializeField] private UI_CardInDeck _cardPrefab;

        [Header("按钮与协作")] [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _peekButton;
        [SerializeField] private UI_HandView _handView;

        [Header("手感")] [SerializeField] private float _flyDuration = 0.22f;
        [SerializeField] private float _confirmPopDuration = 0.2f;

        private MonoObjectPool<UI_CardInDeck> _selectedPool;
        private MonoObjectPool<UI_CardInDeck> _unupgradePool;
        private MonoObjectPool<UI_CardInDeck> _upgradedPool;
        private readonly List<CardModel> _selected = new();
        private readonly List<CardModel> _candidates = new();
        private readonly Dictionary<CardModel, UI_CardInDeck> _displayOf = new();
        private readonly List<UI_CardInDeck> _upgradePreviews = new();
        private UniTaskCompletionSource<IReadOnlyList<CardModel>> _result;
        private ECardSelectMode _mode;
        private int _targetCount;
        private bool _peeking;
        private bool _open;
        private bool _listenersAdded;
        private Transform _peekHome;
        private int _peekHomeIndex;

        private RectTransform DisplayGroup => _mode == ECardSelectMode.Upgrade ? _unupgradeGroup : _selectedGroup;

        private MonoObjectPool<UI_CardInDeck> DisplayPool =>
            _mode == ECardSelectMode.Upgrade ? _unupgradePool : _selectedPool;

        private void OnEnable() => EnsureListeners();

        private void OnDisable()
        {
            if (_listenersAdded)
            {
                _confirmButton.onClick.RemoveListener(OnConfirm);
                _peekButton.onClick.RemoveListener(TogglePeek);
                _listenersAdded = false;
            }

            AbortIfPending();
        }

        private void OnDestroy()
        {
            if (_result == null) return;
            _result.TrySetResult(Array.Empty<CardModel>());
            _result = null;
        }

        private void EnsureListeners()
        {
            if (_listenersAdded) return;
            _confirmButton.onClick.AddListener(OnConfirm);
            _peekButton.onClick.AddListener(TogglePeek);
            _listenersAdded = true;
        }

        // 外部失活（战斗结束 / 场景卸载）时兜底交付空列表，避免域侧等待源永久挂死
        private void AbortIfPending()
        {
            if (_result == null) return;
            _result.TrySetResult(Array.Empty<CardModel>());
            _result = null;
            ResetPanel(true);
        }

        [Inject]
        public void Construct(IObjectResolver resolver_, BattleEventDispatcher eventDispatcher_)
        {
            _selectedPool = CreatePool(_selectedGroup, resolver_);
            _unupgradePool = CreatePool(_unupgradeGroup, resolver_);
            _upgradedPool = CreatePool(_upgradedGroup, resolver_);
            eventDispatcher_.Register<BE_CardSelectionRequested>(OnRequestedAsync);
        }

        #region 打开选牌阶段：只陈列域侧算好的候选，等待玩家确认时交回列表

        private async UniTask OnRequestedAsync(BE_CardSelectionRequested requested_)
        {
            _mode = requested_.Mode;
            _targetCount = requested_.TargetCount;
            _tipText.text = _mode == ECardSelectMode.Upgrade
                ? $"选择手牌中的 <color=yellow>{_targetCount}</color> 张牌进行升级"
                : $"选择手牌中的至多 <color=yellow>{_targetCount}</color> 张牌进行消耗";
            _candidates.Clear();
            _candidates.AddRange(requested_.Candidates);
            _result = requested_.Result;
            _peeking = false;
            _open = true;
            _background.SetActive(true);
            _selectSection.SetActive(_mode == ECardSelectMode.Exhaust);
            _upgradeSection.SetActive(_mode == ECardSelectMode.Upgrade);
            _confirmButton.gameObject.SetActive(false);
            Show();
            EnsureListeners();
            _handView.SetSelectMode(true, OnCardSelected);
            RefreshConfirm();
            await _result.Task;
        }

        #endregion

        #region 选牌：选中飞入组内、点击组内卡取消、满员后点新牌顶替最早选中的

        private void OnCardSelected(CardModel card_)
        {
            if (!_open || _peeking || !_candidates.Contains(card_) || _selected.Contains(card_)) return;
            if (_selected.Count >= _targetCount) CancelAt(0);

            _handView.TryTakeForSelection(card_, out Vector3 position, out Quaternion rotation, out float scale);
            _selected.Add(card_);
            int index = _selected.Count - 1;
            _displayOf[card_] = SpawnCard(DisplayGroup, DisplayPool, card_, OnDisplayClicked, position, rotation,
                scale, index);
            if (_mode == ECardSelectMode.Upgrade)
            {
                CardModel upgraded = card_.ToMutable();
                upgraded.Upgrade();
                _upgradePreviews.Add(SpawnCard(_upgradedGroup, _upgradedPool, upgraded, null, position, rotation,
                    scale, index));
            }

            RefreshConfirm();
        }

        private void OnDisplayClicked(CardModel card_)
        {
            int index = _selected.IndexOf(card_);
            if (index >= 0) CancelAt(index);
        }

        private void CancelAt(int index_)
        {
            CardModel card = _selected[index_];
            _selected.RemoveAt(index_);
            if (_displayOf.Remove(card, out UI_CardInDeck display)) DisplayPool.Release(display);
            if (_mode == ECardSelectMode.Upgrade && index_ < _upgradePreviews.Count)
            {
                _upgradedPool.Release(_upgradePreviews[index_]);
                _upgradePreviews.RemoveAt(index_);
            }

            _handView.ReturnFromSelection(card);
            for (int i = 0; i < _selected.Count; i++)
            {
                _displayOf[_selected[i]].transform.SetSiblingIndex(i);
                if (_mode == ECardSelectMode.Upgrade) _upgradePreviews[i].transform.SetSiblingIndex(i);
            }

            RefreshConfirm();
        }

        #endregion

        #region 确认与 peek：确认是唯一出口，peek 只收起面板自身内容

        private void OnConfirm()
        {
            if (!_open || _peeking) return;
            List<CardModel> result = new(_selected);
            if (_mode == ECardSelectMode.Exhaust)
                foreach (CardModel card in result)
                    if (_displayOf.TryGetValue(card, out UI_CardInDeck display))
                        _handView.RevealFromSelection(card, display.transform.position, display.transform.rotation);

            _result.TrySetResult(result);
            _result = null;
            ResetPanel(false);
            Hide();
        }

        private void TogglePeek()
        {
            if (!_open) return;
            _peeking = !_peeking;
            _background.SetActive(!_peeking);
            _selectSection.SetActive(!_peeking && _mode == ECardSelectMode.Exhaust);
            _upgradeSection.SetActive(!_peeking && _mode == ECardSelectMode.Upgrade);
            _confirmButton.gameObject.SetActive(!_peeking &&
                (_mode == ECardSelectMode.Exhaust || _selected.Count >= _targetCount));
            _handView.SetSelectMode(true, _peeking ? null : OnCardSelected);
            if (_peeking)
            {
                // 全工程唯一的运行期提层例外：牌堆看板的全屏黑底会盖住返回用的 peek 按钮，故临时提到本画布末位；
                // 其余层级一律静态，勿再新增运行期提层
                _peekHome = _peekButton.transform.parent;
                _peekHomeIndex = _peekButton.transform.GetSiblingIndex();
                _peekButton.transform.SetParent(transform.parent, true);
                _peekButton.transform.SetAsLastSibling();
            }
            else RestorePeekButton();
        }

        private void RestorePeekButton()
        {
            if (!_peekHome) return;
            _peekButton.transform.SetParent(_peekHome, true);
            _peekButton.transform.SetSiblingIndex(_peekHomeIndex);
            _peekHome = null;
        }

        #endregion

        #region 收摊与陈列辅助

        private void ResetPanel(bool returnCards_)
        {
            RestorePeekButton();
            foreach (CardModel card in _selected)
            {
                if (_displayOf.TryGetValue(card, out UI_CardInDeck display)) DisplayPool.Release(display);
                if (returnCards_) _handView.ReturnFromSelection(card);
            }

            foreach (UI_CardInDeck preview in _upgradePreviews) _upgradedPool.Release(preview);
            _displayOf.Clear();
            _upgradePreviews.Clear();
            _selected.Clear();
            _candidates.Clear();
            _handView.SetSelectMode(false, null);
            _peeking = false;
            _open = false;
            _background.SetActive(true);
            _selectSection.SetActive(true);
            _upgradeSection.SetActive(true);
            _confirmButton.gameObject.SetActive(false);
        }

        private void RefreshConfirm()
        {
            bool ready = _mode == ECardSelectMode.Exhaust || _selected.Count >= _targetCount;
            if (ready == _confirmButton.gameObject.activeSelf) return;
            if (!ready)
            {
                _confirmButton.gameObject.SetActive(false);
                return;
            }

            // 先写 1 再激活，免得同节点的 HoverScaler 把激活瞬间的 0 缓存成基准缩放
            _confirmButton.transform.localScale = Vector3.one;
            _confirmButton.gameObject.SetActive(true);
            _confirmButton.transform.localScale = Vector3.zero;
            _confirmButton.transform.DOScale(1f, _confirmPopDuration);
        }

        private static MonoObjectPool<UI_CardInDeck> CreatePool(Transform parent_, IObjectResolver resolver_) =>
            new(parent_, 16, onCreate_: instance_ => resolver_.InjectGameObject(instance_.gameObject));

        private UI_CardInDeck SpawnCard(RectTransform group_, MonoObjectPool<UI_CardInDeck> pool_, CardModel card_,
            Action<CardModel> onClick_, Vector3 from_, Quaternion fromRotation_, float fromScale_, int index_)
        {
            UI_CardInDeck instance = pool_.Get(_cardPrefab);
            instance.transform.SetSiblingIndex(index_);
            instance.Bind(card_, onClick_);
            LayoutRebuilder.ForceRebuildLayoutImmediate(group_);

            RectTransform rect = (RectTransform)instance.transform;
            Vector3 target = rect.position;
            Quaternion targetRotation = rect.rotation;
            float targetScale = rect.localScale.x;
            rect.position = from_;
            rect.rotation = fromRotation_;
            rect.localScale = Vector3.one * fromScale_;
            rect.DOMove(target, _flyDuration);
            rect.DORotateQuaternion(targetRotation, _flyDuration);
            rect.DOScale(Vector3.one * targetScale, _flyDuration);
            return instance;
        }

        #endregion
    }
}
