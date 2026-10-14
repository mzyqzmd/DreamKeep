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
    /// 说明气泡（弹出时机由调用方定，悬停类与拖拽类同为一路：内容、锚点、对齐点与挂载宿主都由调用方给，此处只管实例取还、对齐与叠列定位）
    /// 与回合横幅（公告类：自订阅 BE_TurnStarted 转发展示）；运行期不认识具体调用方（仅装配期注入事件门面）。</summary>
    public sealed class UI_PopTextRoot : MonoBehaviour
    {
        [SerializeField] private UI_DescriptionPopTip _descriptionPopTipPrefab;
        [SerializeField] private UI_TurnCountPopTip _turnCountPopTipPrefab;

        [Tooltip("叠列气泡之间的垂直间距（画布像素）：同一张卡的多条词条说明一条压一条地往下排")] [SerializeField]
        private float _stackSpacingPx = 12f;

        private UI_TurnCountPopTip _turnCountPopTip;
        private readonly List<UI_DescriptionPopTip> _shownTips = new(); // 当前这一列的展示名单（按叠列顺序）：归还只认它
        private MonoObjectPool<UI_DescriptionPopTip> _tipPool;
        private object _tipsRequester;

        private void Awake() => _tipPool = new MonoObjectPool<UI_DescriptionPopTip>(transform);

        #region 说明气泡：同一发起者的一条或多条说明按锚点与对齐点排开

        /// <summary>展示说明气泡：调用方给内容列表、世界锚点、气泡以自身何处贴锚点（EUiAnchor）与挂载宿主（不传则挂本宿主自身）；
        /// 第一条以该对齐点落在锚点上，其后各条按列表顺序朝锚点的反方向叠列（底沿贴锚点向上叠，顶沿与居中贴锚点向下叠）；
        /// 整列挂在宿主名下，于是随宿主的层级显示、被宿主带着移动与缩放；内容与列位都在这一次调用里刷完（换发起者或换内容时调用）。</summary>
        public void ShowTips(object requester_, IReadOnlyList<(string Name, string Description)> tips_, Vector3 worldPosition_,
            EUiAnchor tipAnchor_, Transform parent_ = null)
        {
            _tipsRequester = requester_;
            ClearTips(); // 上一列整列归还：气泡不按列位常驻，每次展示都是一轮取用

            Vector2 pivot = UiGeometry.Pivot(tipAnchor_);
            Transform host = parent_ ? parent_ : transform;
            for (int i = 0; i < tips_.Count; i++)
            {
                UI_DescriptionPopTip tip = _tipPool.Get(_descriptionPopTipPrefab);
                tip.SetContent(tips_[i].Name, tips_[i].Description);
                ((RectTransform)tip.transform).pivot = pivot;
                tip.transform.SetParent(host, false); // 保留本地姿态：气泡尺寸随宿主缩放，位置紧接着按世界锚点重写
                tip.transform.SetAsLastSibling();
                _shownTips.Add(tip);
            }

            PlaceStackTips(worldPosition_, pivot);
        }

        /// <summary>收起整列：只认当前发起者（气泡随归还失活，下次展示时再取用）。</summary>
        public void HideTips(object requester_)
        {
            if (_tipsRequester != requester_) return;
            _tipsRequester = null;
            ClearTips();
        }

        #endregion

        // 整列归还池：归还即失活入桶，展示名单同时清空
        private void ClearTips()
        {
            for (int i = 0; i < _shownTips.Count; i++) _tipPool.Release(_shownTips[i]);
            _shownTips.Clear();
        }

        // 按对齐点排位：底沿贴锚点（pivot 的 y 为 0）整列向上长，顶沿与居中则向下长；
        // 每条写完内容先强制重排拿到当帧高度，再用它自己的矩形算出下一条的锚点（TransformPoint 自带缩放换算），故气泡高矮不一也能一条贴一条地排下去
        private void PlaceStackTips(Vector3 worldAnchor_, Vector2 pivot_)
        {
            Vector3 anchor = worldAnchor_;
            float step = pivot_.y > 0f ? -1f : 1f;
            for (int i = 0; i < _shownTips.Count; i++)
            {
                RectTransform rect = (RectTransform)_shownTips[i].transform;
                rect.position = anchor;
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                anchor = rect.TransformPoint(new Vector3(0f, step * (rect.rect.height + _stackSpacingPx), 0f));
            }
        }

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
