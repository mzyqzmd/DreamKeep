using System;
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
    /// <summary>卡牌多选弹板的抽象基类：把「陈列候选卡 → 左键 FIFO 多选（涟漪开关）→ 选满弹出确认键 → 右键开卡牌特写 →
    /// 确认/取消 → 横向均布演出 → 交还结果」这套流程收在一处，子类只回答四个问题：
    /// 候选卡是哪批（CollectCandidates）、确认后做什么（OnConfirm）、演出播哪个（PlayVfx）、面板特有陈列怎么摆（OnShownExtra）。
    /// 目标张数一律钳到候选卡数：为 0 时开板即视作「选满」，确认键直接弹出、点它交还空列表，
    /// 保证「没有可选的卡」这类极端输入不会把调用方卡住。开合能力继承自 UI_Panel，列表构建落在 OnShown。</summary>
    public abstract class UI_CardSelectPanel : UI_Panel
    {
        [Header("列表")] [SerializeField] private Transform _scrollContent;
        [SerializeField] private UI_CardInDeck _cardPrefab;

        [Header("确认按钮")] [Tooltip("选满目标张数才缩放弹出；目标为 0 时开板即弹出")] [SerializeField]
        private Button _confirmButton;

        [Tooltip("弹出/收起补间时长")] [SerializeField] private float _confirmPopDuration = 0.2f;

        [Header("演出")] [Tooltip("多个演出的整体缩放")] [SerializeField] private float _vfxScale = 1f;

        [Tooltip("多个演出的横向间距（画布像素，经面板父级换算成世界偏移）")] [SerializeField]
        private float _vfxSpacingPx = 500f;

        [Header("开合")] [SerializeField] private Button _closeButton;

        private bool _cancelable = true; // 拾取型遗物的强制选牌用：false 时藏起关闭键并拦住取消路径

        [Header("右键详情")] [Tooltip("统一卡牌详情特写：右键选卡打开，点黑底关闭")] [SerializeField]
        private UI_ConcreteCardView _concrete;

        private IObjectResolver _resolver;
        private VfxFactory _vfxFactory;
        private MonoObjectPool<UI_CardInDeck> _pool;
        private readonly List<UI_CardInDeck> _activeItems = new();
        private readonly List<UI_CardInDeck> _selected = new(); // FIFO 真源：先选中的先被替换
        private Player _player;
        private int _requestedCount;
        private int _target;
        private UniTaskCompletionSource<IReadOnlyList<CardModel>> _result;
        private Tween _confirmTween;
        private bool _confirmShown;

        // 子类可见的上下文：筛候选、写提示、摆第二层都要读
        protected Player Player => _player;
        protected int Target => _target;
        protected IReadOnlyList<UI_CardInDeck> Selected => _selected;
        protected UI_CardInDeck CardPrefab => _cardPrefab;
        protected VfxFactory VfxFactory => _vfxFactory;

        private void Awake()
        {
            _confirmButton.onClick.AddListener(OnConfirmClicked);
            _closeButton.onClick.AddListener(OnPanelClose);
            OnAwake();
        }

        // 外部失活兜底：清场与复位（不抢结果交付）
        private void OnDisable() => ResetPanel();

        private void OnDestroy()
        {
            _confirmButton.onClick.RemoveListener(OnConfirmClicked);
            _closeButton.onClick.RemoveListener(OnPanelClose);
            _confirmTween?.Kill();
            OnDestroyed();
        }

        // 注入即装配：PopPanelFactory 的 InjectGameObject 与 RegisterComponentInHierarchy 都走这里，
        // 池在首次开板前就已就位（父节点是面板自己的子节点，不依赖 Awake 跑过）
        [Inject]
        public void Construct(IObjectResolver resolver_)
        {
            _resolver = resolver_;
            _vfxFactory = _resolver.Resolve<VfxFactory>();
            _pool = CreateCardPool(_scrollContent);
            OnConstructed();
        }

        /// <summary>玩家按下确认、且确实有牌要处理的那一刻上报一次（在子类 OnConfirm 之前）：
        /// 供外部做"开始处理"的表现（如商店删牌服务位的翻面）。目标为 0 的空确认不上报。</summary>
        public event Action Confirmed;

        #region 对外：陈列候选卡并等待玩家决定

        /// <summary>按请求张数陈列候选卡：实际目标张数会被钳到候选卡数。</summary>
        public void Show(Player player_, int count_)
        {
            _player = player_;
            _requestedCount = count_;
            Show(); // 交由 UI_Panel.Show 触发 OnShown 构建列表
        }

        /// <summary>等玩家做完：null = 关闭取消；空列表 = 完成但一张都没选。</summary>
        public async UniTask<IReadOnlyList<CardModel>> WaitResultAsync()
        {
            _result = new UniTaskCompletionSource<IReadOnlyList<CardModel>>();
            return await _result.Task;
        }

        /// <summary>是否允许取消：false 时隐藏关闭键并拦住取消路径，玩家不能空手离场（拾起时代价必须付完）。
        /// 面板是单例复用的，复位在 ResetPanel（开板与失活兜底共用）——调用方须在 Show 之后再调本方法。</summary>
        public void SetCancelable(bool cancelable_)
        {
            _cancelable = cancelable_;
            _closeButton.gameObject.SetActive(cancelable_);
        }

        #endregion

        protected override void OnShown()
        {
            ResetPanel();

            IReadOnlyList<CardModel> candidates = CollectCandidates();
            _target = Mathf.Clamp(_requestedCount, 0, candidates.Count);
            for (int i = 0; i < candidates.Count; i++)
            {
                CardModel card = candidates[i];
                UI_CardInDeck item = _pool.Get(_cardPrefab);
                item.transform.SetSiblingIndex(i); // 池复用的兄弟位是陈设序：取出即按陈列序钉位
                item.Bind(card, _ => ToggleCard(item), _ => _concrete.Open(card));
                _activeItems.Add(item);
            }

            OnShownExtra();
            ShowConfirm(_selected.Count == _target); // 目标为 0 即开板就算「选满」
        }

        #region 子类钩子：只回答「选哪些卡、确认后做什么、演出播哪个」

        /// <summary>本次可选的候选卡（决定网格陈列与目标张数上限）。</summary>
        protected abstract IReadOnlyList<CardModel> CollectCandidates();

        /// <summary>已选满目标张数且目标大于 0 时的确认动作：写域与演出由子类编排，最终调 FinishAsync 交付。</summary>
        protected abstract void OnConfirm();

        /// <summary>单个卡牌的演出投放（横向均布的位置与缩放由基类算好）。</summary>
        protected abstract void PlayVfx(Transform parent_, Vector3 worldPosition_, float scale_, CardModel card_,
            Action onFinished_);

        #endregion

        #region 子类可选口：绑自己的按钮、追加池、写提示、收自己的层

        /// <summary>Awake 末尾回调：绑定子类特有按钮（此时 Inspector 引用已就位）。</summary>
        protected virtual void OnAwake() { }

        /// <summary>OnDestroy 末尾回调：解绑 OnAwake 里绑的按钮。</summary>
        protected virtual void OnDestroyed() { }

        /// <summary>Construct 末尾回调：追加子类自己的对象池（网格池此刻已建好）。</summary>
        protected virtual void OnConstructed() { }

        /// <summary>开板铺完网格后回调：写提示文案等子类特有陈列。</summary>
        protected virtual void OnShownExtra() { }

        /// <summary>每次清场时回调：收起子类特有层（如第二层对照预览）。</summary>
        protected virtual void OnResetVisual() { }

        #endregion

        #region 子类可用的编排件：取选中卡、收场交付

        /// <summary>取走当前选中卡（按选中顺序）并清空选中表：确认动作先调它，之后同帧再点也不会重复处理同一张。</summary>
        protected List<CardModel> TakeSelectedCards()
        {
            List<CardModel> cards = new(_selected.Count);
            for (int i = 0; i < _selected.Count; i++) cards.Add(_selected[i].Card);
            _selected.Clear();
            return cards;
        }

        /// <summary>收场并交付：关板 → 横向均布演出一遍 → 交还结果；演出异常也交付，调用方不会永久挂住。</summary>
        protected async UniTask FinishAsync(IReadOnlyList<CardModel> cards_)
        {
            UniTaskCompletionSource<IReadOnlyList<CardModel>> pending = _result; // 演出期间结果槽不被下一轮覆盖
            _selected.Clear();
            Hide(); // 确认即整板收起：背景、列表与子类特有层一并消失
            try
            {
                await PlaySpreadVfxAsync(cards_);
            }
            finally
            {
                pending?.TrySetResult(cards_);
            }
        }

        #endregion

        /// <summary>建一个卡视图池：新实例统一补容器注入（子类追加第二层两排池时复用）。</summary>
        protected MonoObjectPool<UI_CardInDeck> CreateCardPool(Transform parent_) =>
            new(parent_, 64, onCreate_: instance_ => _resolver.InjectGameObject(instance_.gameObject));

        // 左键选择：已选即取消；未选未满即加入；未选已满按 FIFO 踢掉最早选中的
        private void ToggleCard(UI_CardInDeck item_)
        {
            if (_selected.Contains(item_))
            {
                _selected.Remove(item_);
                item_.SetRippleVisible(false);
            }
            else if (_selected.Count < _target)
            {
                _selected.Add(item_);
                item_.SetRippleVisible(true);
            }
            else
            {
                if (_selected.Count == 0) return; // 目标为 0：一张都不该能选中
                UI_CardInDeck oldest = _selected[0];
                _selected.RemoveAt(0);
                oldest.SetRippleVisible(false);
                _selected.Add(item_);
                item_.SetRippleVisible(true);
            }

            ShowConfirm(_selected.Count == _target);
        }

        // 确认键显隐：选满弹出、不满收起，同态零操作（等值短路）
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
                        1f, _confirmPopDuration, Ease.OutBack)
                    .OnComplete(() => _confirmTween = null); // 播完即弃引用：Tween 会被回收复用，留着再 Kill 会误杀别人
            }
            else
            {
                _confirmTween = UiTween.PlayScalar(ref _confirmTween, ReadConfirmScale, WriteConfirmScale,
                        0f, _confirmPopDuration, Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        _confirmTween = null;
                        _confirmButton.gameObject.SetActive(false);
                    });
            }
        }

        private float ReadConfirmScale() => _confirmButton.transform.localScale.x;

        private void WriteConfirmScale(float value_) =>
            _confirmButton.transform.localScale = new Vector3(value_, value_, 1f);

        // 复位到隐藏态：杀补间、缩放归零并失活（开板与失活兜底共用）
        private void ResetConfirm()
        {
            _confirmTween?.Kill();
            _confirmShown = false;
            WriteConfirmScale(0f);
            _confirmButton.gameObject.SetActive(false);
        }

        // 关闭键：取消，面板自己收起（调用方只管结果），交还 null 由入口回到原流程；不可取消时整条路径拦住
        private void OnPanelClose()
        {
            if (!_cancelable) return;
            Hide();
            _result?.TrySetResult(null);
        }

        // 确认入口：选满才放行；目标为 0 时直接完成（不演出），其余交子类
        private void OnConfirmClicked()
        {
            if (_selected.Count < _target) return;
            if (_target == 0)
            {
                Hide();
                _result?.TrySetResult(Array.Empty<CardModel>());
                return;
            }

            Confirmed?.Invoke(); // 先播"开始处理"的表现，再进子类的删牌/升级流程
            OnConfirm();
        }

        // 演出：N 个以面板父级（弹层画布容器）中心为基准横向均布，同帧开播同时结束；
        // 完成回调全指向同一完成源（TrySetResult 幂等），任一完成即全部完成；工厂预制体缺失会立即回调不挂死
        private UniTask PlaySpreadVfxAsync(IReadOnlyList<CardModel> cards_)
        {
            if (cards_.Count == 0) return UniTask.CompletedTask; // 没有卡就没有演出，直接放行
            UniTaskCompletionSource done = new();
            Transform parent = transform.parent;
            Vector3 center = parent.position;
            for (int i = 0; i < cards_.Count; i++)
            {
                float shift = (i - (cards_.Count - 1) / 2f) * _vfxSpacingPx;
                Vector3 worldPos = center + parent.TransformVector(new Vector3(shift, 0f, 0f));
                PlayVfx(parent, worldPos, _vfxScale, cards_[i], () => done.TrySetResult());
            }

            return done.Task;
        }

        // 清场：特写、子类特有层、网格台账与确认键一并复位，选中表清空（开板与失活兜底共用）；
        // 取消许可也复位为 true——强制选牌是调用方的一次性要求，不该泄漏给下一次开板
        private void ResetPanel()
        {
            _selected.Clear(); // 视图已还池：别把池实例留在选中表里被下次误用
            if (_concrete) _concrete.Close();
            OnResetVisual();
            ReleaseItems();
            ResetConfirm();
            SetCancelable(true);
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
