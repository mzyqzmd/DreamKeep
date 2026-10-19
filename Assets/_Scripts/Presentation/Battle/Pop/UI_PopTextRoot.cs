using System.Collections.Generic;
using _DreamKeep.GameBattle;
using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>弹层宿主：挂在场景 Canvas_Pop/PopTextRoot 上，持有本战斗的弹层文本——
    /// 说明气泡的"实例取还与唯一列守卫"归此处（同一时刻只许一列、谁的归还只收自己那列），叠列定位由消费方 UI_HoverTip 接管；
    /// 数字飘字同样在这里取还实例，弹什么由 UI_NumberPopDirector 决定；回合横幅（公告类）自订阅 BE_TurnStarted 转发展示。
    /// 运行期不认识具体调用方（仅装配期注入事件门面）。</summary>
    public sealed class UI_PopTextRoot : MonoBehaviour
    {
        [SerializeField] private UI_DescriptionPopTip _descriptionPopTipPrefab;
        [SerializeField] private UI_TurnCountPopTip _turnCountPopTipPrefab;
        [Tooltip("数字/格挡飘字预制体：由 UI_NumberPopDirector 按语义驱动弹出")] [SerializeField]
        private UI_NumberPopTip _numberPopTipPrefab;

        private MonoObjectPool<UI_DescriptionPopTip> _tipPool;
        private MonoObjectPool<UI_NumberPopTip> _numberPopTipPool;
        private readonly List<UI_DescriptionPopTip> _shownTips = new(); // 当前这一列的展示名单（按叠列顺序）：归还只认它
        private object _tipsRequester;

        private Camera _canvasCamera;
        private UI_TurnCountPopTip _turnCountPopTip;

        private void Awake()
        {
            _tipPool = new MonoObjectPool<UI_DescriptionPopTip>(transform);
            _numberPopTipPool = new MonoObjectPool<UI_NumberPopTip>(transform, onReturn_: tip_ => tip_.ResetView());
            _canvasCamera = GetComponentInParent<Canvas>().worldCamera;
        }

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

        #region 数字飘字：实例取还与世界锚点落到本画布平面

        /// <summary>弹一条数字飘字：text_ 为显示文字，color_ 为文字颜色，worldPosition_ 为单位头顶等世界锚点；
        /// 实例播完自动还池，新弹出的永远压在宿主其它弹层之上。</summary>
        public void ShowNumber(string text_, Color color_, ENumberPopTipType type_, Vector3 worldPosition_)
        {
            UI_NumberPopTip tip = _numberPopTipPool.Get(_numberPopTipPrefab);
            tip.transform.SetAsLastSibling();
            // 世界点先投影成屏幕点、再投回本画布平面：平面距离与透视差异都被吃掉，只借用它的位置
            Vector3 screenPoint = _canvasCamera.WorldToScreenPoint(worldPosition_);
            if (!RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)transform, screenPoint,
                    _canvasCamera, out Vector3 canvasPoint)) return;
            tip.Play(text_, color_, type_, canvasPoint, () => _numberPopTipPool.Release(tip));
        }

        #endregion

        #region 回合横幅弹出

        [Inject]
        public void Construct(BattleEventDispatcher eventDispatcher_) =>
            eventDispatcher_.Register<BE_TurnStarted>(OnTurnStartedAsync);


        private UniTask OnTurnStartedAsync(BE_TurnStarted started_)
        {
            if (_turnCountPopTip == null) _turnCountPopTip = Instantiate(_turnCountPopTipPrefab, transform);
            _turnCountPopTip.Show(started_.IsPlayerTurn, started_.TurnCount);
            return UniTask.CompletedTask;
        }

        #endregion
    }
}