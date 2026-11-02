using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;
using _DreamKeep.EngineUtility;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>卡牌移除弹板：陈列局内卡组全部卡，左键选中即开 card_ripple 涟漪、再点取消；
    /// 选满指定张数时右下角确认按钮缩放弹出、未满收起，选满后再点未选卡按 FIFO 替换最早选中的；
    /// 右键打开统一卡牌详情特写。确认即逐张从卡组移除并关板，被删卡以删牌 VFX 在弹层画布上
    /// 横向均布同时演出，播完交还被删卡列表；关闭键为取消语义交还 null。
    /// 由 PopPanelFactory 懒建单例、跨段复用（容器外实例化后补注入）；开合能力继承自 UI_Panel，列表构建落在 OnShown。</summary>
    public sealed class UI_CardRemovePanel : UI_Panel
    {
        [Header("列表")] [SerializeField] private Transform _scrollContent;
        [SerializeField] private UI_CardInDeck _cardPrefab;

        [Header("确认按钮")] [Tooltip("右下角确认：选满指定张数才弹出，缩放弹出/收起由本面板驱动")] [SerializeField]
        private Button _confirmButton;

        [Tooltip("弹出/收起补间时长")] [SerializeField] private float _confirmPopDuration = 0.2f;

        [Header("删牌演出")] [Tooltip("多个删牌 VFX 横向均布的相邻间距（画布像素，经面板父级换算成世界偏移）")] [SerializeField]
        private float _vfxSpacingPx = 500f;

        [Tooltip("删牌 VFX 整体缩放")] [SerializeField] private float _vfxScale = 1f;

        [Header("开合")] [SerializeField] private Button _closeButton;

        [Header("右键详情")] [Tooltip("统一卡牌详情特写：右键选卡打开，接线口径同锻造面板")] [SerializeField]
        private UI_ConcreteCardView _concrete;

        private IObjectResolver _resolver;
        private VfxFactory _vfxFactory;
        private MonoObjectPool<UI_CardInDeck> _pool;
        private readonly List<UI_CardInDeck> _activeItems = new();
        private readonly List<UI_CardInDeck> _selected = new(); // FIFO 真源：先选中的先被替换
        private Player _player;
        private int _removeCount;
        private UniTaskCompletionSource<IReadOnlyList<CardModel>> _result;
        private Tween _confirmTween;
        private bool _confirmShown;

        private void Awake()
        {
            _confirmButton.onClick.AddListener(OnConfirm);
            _closeButton.onClick.AddListener(OnPanelClose);
        }

        // 外部失活兜底：特写与列表实例一并收掉，池实例不滞留；确认按钮复位隐藏
        private void OnDisable()
        {
            if (_concrete) _concrete.Close();
            ReleaseItems();
            ResetConfirm();
        }

        private void OnDestroy()
        {
            _confirmButton.onClick.RemoveListener(OnConfirm);
            _closeButton.onClick.RemoveListener(OnPanelClose);
            _confirmTween?.Kill();
        }

        // 工厂懒建单例、跨段复用：注入只存 resolver（工厂 CreatePanel 的 InjectGameObject 触发），
        // 池懒建到首次开板——注入时机早于首开，若在此建池，换段重建会丢旧池台账
        [Inject]
        public void Construct(IObjectResolver resolver_)
        {
            _resolver = resolver_;
            _vfxFactory = resolver_.Resolve<VfxFactory>();
        }

        private void EnsurePool()
        {
            if (_pool != null) return;
            _pool = new MonoObjectPool<UI_CardInDeck>(_scrollContent, 64,
                onCreate_: instance_ => _resolver.InjectGameObject(instance_.gameObject));
        }

        #region 对外：展示卡组并等待玩家选出要移除的牌

        public void Show(Player player_, int removeCount_)
        {
            _player = player_;
            _removeCount = removeCount_;
            Show(); // 交由 UI_Panel.Show 触发 OnShown 构建列表
        }

        public async UniTask<IReadOnlyList<CardModel>> WaitResultAsync()
        {
            _result = new UniTaskCompletionSource<IReadOnlyList<CardModel>>();
            return await _result.Task;
        }

        #endregion

        protected override void OnShown()
        {
            EnsurePool();
            _selected.Clear();
            if (_concrete) _concrete.Close();
            ReleaseItems();
            ResetConfirm();

            IReadOnlyList<CardModel> cards = _player.Deck.Cards;
            for (int i = 0; i < cards.Count; i++)
            {
                CardModel card = cards[i];
                UI_CardInDeck item = _pool.Get(_cardPrefab);
                item.transform.SetSiblingIndex(i); // 池复用的兄弟位是陈设序：取出即按陈列序钉位
                item.Bind(card, _ => ToggleCard(item), _ => _concrete.Open(card));
                _activeItems.Add(item);
            }
        }

        // 左键选择：已选即取消；未选未满即加入；未选已满按 FIFO 踢掉最早选中的
        private void ToggleCard(UI_CardInDeck item_)
        {
            if (_selected.Contains(item_))
            {
                _selected.Remove(item_);
                item_.SetRippleVisible(false);
            }
            else if (_selected.Count < _removeCount)
            {
                _selected.Add(item_);
                item_.SetRippleVisible(true);
            }
            else
            {
                UI_CardInDeck oldest = _selected[0];
                _selected.RemoveAt(0);
                oldest.SetRippleVisible(false);
                _selected.Add(item_);
                item_.SetRippleVisible(true);
            }
            ShowConfirm(_selected.Count == _removeCount);
        }

        // 确认按钮显隐：选满弹出、未满收起，同态零操作（等值短路）
        private void ShowConfirm(bool shown_)
        {
            if (_confirmShown == shown_) return;
            _confirmShown = shown_;
            if (shown_)
            {
                // 先把 scale 抬回基准再激活：按钮从隐藏态首次激活时，同节点自驱悬停组件的 Awake 会缓存
                // 当时的 scale 当作悬停基准——不能让它缓存到弹出前的 0，否则悬停补间会把按钮缩回 0
                WriteConfirmScale(1f);
                _confirmButton.gameObject.SetActive(true);
                WriteConfirmScale(0f); // 从零弹出
                _confirmTween = UiTween.PlayScalar(ref _confirmTween, ReadConfirmScale, WriteConfirmScale,
                    1f, _confirmPopDuration, Ease.OutBack);
            }
            else
            {
                _confirmTween = UiTween.PlayScalar(ref _confirmTween, ReadConfirmScale, WriteConfirmScale,
                    0f, _confirmPopDuration, Ease.OutQuad)
                    .OnComplete(() => _confirmButton.gameObject.SetActive(false));
            }
        }

        private float ReadConfirmScale() => _confirmButton.transform.localScale.x;

        private void WriteConfirmScale(float value_) => _confirmButton.transform.localScale = new Vector3(value_, value_, 1f);

        // 复位到隐藏态：杀补间、缩放归零并失活（开板与失活兜底共用）
        private void ResetConfirm()
        {
            _confirmTween?.Kill();
            _confirmShown = false;
            WriteConfirmScale(0f);
            _confirmButton.gameObject.SetActive(false);
        }

        // 确认移除：先取走选中态再关板（同帧手快再敲确认时 _selected 已空），域删除后关板并播删牌演出
        private async void OnConfirm()
        {
            if (_selected.Count < _removeCount) return;
            List<CardModel> removed = new(_selected.Count);
            for (int i = 0; i < _selected.Count; i++) removed.Add(_selected[i].Card);
            _selected.Clear();

            for (int i = 0; i < removed.Count; i++) _player.Deck.Remove(removed[i]);

            Hide(); // 确认即整板收起：背景、文本、网格一并消失
            await PlayRemoveVfxAsync(removed);
            _result?.TrySetResult(removed);
        }

        // 面板关闭键：取消移除，交还 null 由调用方回到原流程
        private void OnPanelClose() => _result?.TrySetResult(null);

        // 删牌演出：N 个 VFX 以面板父级（弹层画布容器）中心为基准横向均布，同帧开播同时结束；
        // 完成回调全指向同一完成源（TrySetResult 幂等），任一完成即全部完成；工厂预制体缺失会立即回调不挂死
        private UniTask PlayRemoveVfxAsync(IReadOnlyList<CardModel> removed_)
        {
            if (removed_.Count == 0) return UniTask.CompletedTask;
            UniTaskCompletionSource done = new();
            Transform parent = transform.parent;
            Vector3 center = parent.position;
            for (int i = 0; i < removed_.Count; i++)
            {
                float shift = (i - (removed_.Count - 1) / 2f) * _vfxSpacingPx;
                Vector3 worldPos = center + parent.TransformVector(new Vector3(shift, 0f, 0f));
                _vfxFactory.PlayRemoveCard(parent, worldPos, _vfxScale, removed_[i], () => done.TrySetResult());
            }
            return done.Task;
        }

        private void ReleaseItems()
        {
            if (_pool == null) return;
            for (int i = _activeItems.Count - 1; i >= 0; i--)
                if (_activeItems[i]) _pool.Release(_activeItems[i]);
            _activeItems.Clear();
        }
    }
}