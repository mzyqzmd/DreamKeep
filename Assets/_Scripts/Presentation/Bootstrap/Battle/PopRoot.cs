using System.Collections.Generic;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>战斗弹层中枢：挂在场景 GameRoot 上，统一持有本战斗的弹层文本——
    /// 说明气泡的"实例取还与唯一列守卫"归此处（同一时刻只许一列、谁的归还只收自己那列），叠列定位由消费方 UI_HoverTip 接管；
    /// 数字飘字与回合横幅由本组件直接订阅域事件、按语义决定弹什么，实例沿用本组件的池。
    /// 弹层一律挂到"所属对象"之下（数字→单位视图、气泡→发起者指定的被描述对象），只有回合横幅由 Inspector 配默认父级。</summary>
    public sealed class PopRoot : MonoBehaviour
    {
        [SerializeField] private UI_DescriptionPopTip _descriptionPopTipPrefab;
        [SerializeField] private UI_TurnCountPopTip _turnCountPopTipPrefab;
        [Tooltip("数字/格挡飘字预制体：按伤害与回血事件弹在单位头顶")] [SerializeField]
        private UI_NumberPopTip _numberPopTipPrefab;

        [Header("数字飘字")] [SerializeField] private Color _damageColor = new(1f, 0.28f, 0.22f);
        [SerializeField] private Color _healColor = new(0.35f, 0.92f, 0.45f);
        [SerializeField] private Color _blockColor = new(0.55f, 0.82f, 1f);

        [Tooltip("相对骨架顶沿的抬升（画布像素）")] [SerializeField]
        private float _liftPx = 40f;

        [Tooltip("数字起点的横向随机抖动（画布像素）：多段伤害同时出现时错开")] [SerializeField]
        private float _jitterPx = 18f;

        [Header("回合横幅")] [Tooltip("回合横幅的挂载父级：它没有所属对象，只能在这里指定")] [SerializeField]
        private Transform _turnBannerParent;

        private MonoObjectPool<UI_DescriptionPopTip> _tipPool;
        private MonoObjectPool<UI_NumberPopTip> _numberPopTipPool;
        private readonly List<UI_DescriptionPopTip> _shownTips = new(); // 当前这一列的展示名单（按叠列顺序）：归还只认它
        private object _tipsRequester;

        private UI_TurnCountPopTip _turnCountPopTip;
        private UI_ActorRoot _actorRoot;
        private Rng _rng;

        private void Awake()
        {
            _tipPool = new MonoObjectPool<UI_DescriptionPopTip>(transform, onReturn_: ReturnToRoot);
            _numberPopTipPool = new MonoObjectPool<UI_NumberPopTip>(transform, onReturn_: tip_ =>
            {
                ReturnToRoot(tip_);
                tip_.ResetView();
            });
        }

        #region 装配：注入事件门面、单位视图登记表与表现流随机

        [Inject]
        public void Construct(BattleEventDispatcher dispatcher_, UI_ActorRoot actorRoot_, [Key("Presentation")] Rng rng_)
        {
            _actorRoot = actorRoot_;
            _rng = rng_;
            dispatcher_.Register<BE_DamageDealt>(OnDamageDealtAsync);
            dispatcher_.Register<BE_HealthRestored>(OnHealthRestoredAsync);
            dispatcher_.Register<BE_TurnStarted>(OnTurnStartedAsync);
        }

        #endregion

        #region 说明气泡：实例取还与唯一列守卫（叠列定位由 UI_HoverTip 接管）

        /// <summary>为某个发起者取一整列气泡（数量 = count_）：先归还上一列（同一时刻只许一列），
        /// 再从池里取 count_ 条登记到展示名单并返回只读视图，由调用方（UI_HoverTip）去填内容与排位。</summary>
        public IReadOnlyList<UI_DescriptionPopTip> AcquireColumn(object requester_, int count_)
        {
            _tipsRequester = requester_;
            // 顶掉上一列：气泡不按列位常驻，每次展示都是一轮取用
            foreach (var t in _shownTips)
                _tipPool.Release(t);
            _shownTips.Clear();
            for (int i = 0; i < count_; i++) _shownTips.Add(_tipPool.Get(_descriptionPopTipPrefab));
            return _shownTips;
        }

        /// <summary>收起发起者自己那列（已被别人顶掉时不做事）。</summary>
        public void ReleaseColumn(object requester_)
        {
            if (_tipsRequester != requester_) return;
            _tipsRequester = null;
            foreach (var t in _shownTips)
                _tipPool.Release(t);
            _shownTips.Clear();
        }

        #endregion

        // 受伤：实际掉血弹 Toss 数字；有格挡吸收时另弹「格挡！」的 Rise 文案
        private UniTask OnDamageDealtAsync(BE_DamageDealt dealt_)
        {
            int hpLoss = dealt_.Amount - dealt_.Absorbed;
            if (hpLoss <= 0 && dealt_.Absorbed > 0)
                ShowNumber("格挡！", _blockColor, ENumberPopTipType.Rise, dealt_.Target);
            else
                ShowNumber(hpLoss.ToString(), _damageColor, ENumberPopTipType.Toss, dealt_.Target);
            return UniTask.CompletedTask;
        }

        // 回血：域侧不封顶，数字只报真正加上去的那部分；没有实际恢复就不弹
        private UniTask OnHealthRestoredAsync(BE_HealthRestored restored_)
        {
            Creature creature = restored_.Target;
            int actual = Mathf.Min(creature.CurrentHp, creature.MaxHp) -
                         Mathf.Min(creature.CurrentHp - restored_.Amount, creature.MaxHp);
            if (actual <= 0) return UniTask.CompletedTask;
            ShowNumber(actual.ToString(), _healColor, ENumberPopTipType.Rise, creature);
            return UniTask.CompletedTask;
        }

        // 数字飘字：实例挂到单位视图下（随单位一起移动与销毁），用世界锚点直接定位，播完还池
        private void ShowNumber(string text_, Color color_, ENumberPopTipType type_, Creature target_)
        {
            if (!_actorRoot.TryGetView(target_, out UI_Actor view)) return;
            UI_NumberPopTip tip = _numberPopTipPool.Get(_numberPopTipPrefab);
            tip.transform.SetParent(view.transform, false);
            tip.transform.SetAsLastSibling();
            tip.Play(text_, color_, type_, AnchorOf(view), () => _numberPopTipPool.Release(tip));
        }

        // 头顶锚点：骨架矩形顶沿中点再抬高一段，横向抖动沿骨架自身轴向换算
        private Vector3 AnchorOf(UI_Actor view_)
        {
            RectTransform body = (RectTransform)view_.Skeleton.transform;
            float jitter = _rng.Next(-_jitterPx, _jitterPx);
            return UiGeometry.AnchorPoint(body, EUiAnchor.TopCenter) +
                   body.up * (_liftPx * body.lossyScale.y) +
                   body.right * (jitter * body.lossyScale.x);
        }

        // 回合横幅：它没有所属对象，挂到 Inspector 指定的父级；同一时刻只留一个实例，重复触发由横幅自己重播时间线
        private UniTask OnTurnStartedAsync(BE_TurnStarted started_)
        {
            if (_turnCountPopTip == null) _turnCountPopTip = Instantiate(_turnCountPopTipPrefab, _turnBannerParent);
            _turnCountPopTip.transform.SetAsLastSibling();
            _turnCountPopTip.Show(started_.IsPlayerTurn, started_.TurnCount);
            return UniTask.CompletedTask;
        }

        // 还池前归位到本组件名下：空闲实例不长期挂在可能被销毁的单位或槽位下，下次取出再改挂
        private void ReturnToRoot(Component instance_) => instance_.transform.SetParent(transform, false);
    }
}
