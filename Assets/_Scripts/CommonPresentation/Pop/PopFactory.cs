using System;
using System.Collections.Generic;
using _DreamKeep.EngineUtility;
using UnityEngine;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>通用弹层工厂：挂在场景 GameRoot 下，统一持有说明气泡与数字飘字的实例池——
    /// 说明气泡的"实例取还与唯一列守卫"归此处（同一时刻只许一列、谁的归还只收自己那列），叠列定位由消费方 UI_HoverTip 接管；
    /// 数字飘字的父节点与世界坐标由调用方（如战斗弹层编排）算好传入，本类不认识 Creature / UI_ActorRoot / 任何玩法事件；
    /// 遗物信息面板同理只由本类造与存放，写什么内容、什么时候收起都归面板自己。
    /// 弹层一律挂到"所属对象"之下，空闲实例归位到本组件名下。</summary>
    public sealed class PopFactory : MonoBehaviour
    {
        [SerializeField] private UI_DescriptionPopTip _descriptionPopTipPrefab;
        [SerializeField] private UI_NumberPopTip _numberPopTipPrefab;

        [Tooltip("遗物信息面板预制体：点击遗物槽位时弹出")] [SerializeField]
        private UI_RelicsInfoPanel _relicInfoPanelPrefab;

        [Tooltip("遗物信息面板的画布父级：面板随该画布的排序层渲染（如 Canvas_Overlay）")] [SerializeField]
        private RectTransform _relicInfoPanelParent;

        private MonoObjectPool<UI_DescriptionPopTip> _tipPool;
        private MonoObjectPool<UI_NumberPopTip> _numberPopTipPool;
        private readonly List<UI_DescriptionPopTip> _shownTips = new(); // 当前这一列的展示名单（按叠列顺序）：归还只认它
        private object _tipsRequester;
        private UI_RelicsInfoPanel _relicInfoPanel; // 单实例：同一时刻只会有一张信息面板，开合由面板自己管

        private void Awake()
        {
            _tipPool = new MonoObjectPool<UI_DescriptionPopTip>(transform, onReturn_: ReturnToRoot);
            _numberPopTipPool = new MonoObjectPool<UI_NumberPopTip>(transform, onReturn_: tip_ =>
            {
                ReturnToRoot(tip_);
                tip_.ResetView();
            });
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

        #region 数字飘字：父节点与世界坐标由调用方算好传入，播完还池

        /// <summary>在指定父节点下、以给定世界坐标播放一条飘字；播完还池并回调（可为空）。</summary>
        public void SpawnNumber(string text_, Color color_, ENumberPopTipType type_, Transform parent_,
            Vector3 worldPosition_, Action onFinished_ = null)
        {
            UI_NumberPopTip tip = _numberPopTipPool.Get(_numberPopTipPrefab);
            tip.transform.SetParent(parent_, false);
            tip.transform.SetAsLastSibling();
            tip.Play(text_, color_, type_, worldPosition_, () =>
            {
                _numberPopTipPool.Release(tip);
                onFinished_?.Invoke();
            });
        }

        #endregion

        #region 遗物信息面板：单实例懒建，内容与开合归面板自己

        /// <summary>取遗物信息面板：首次调用才按预制体造一张挂到画布父级下，之后一直复用这张。</summary>
        public UI_RelicsInfoPanel AcquireRelicInfoPanel()
        {
            if (!_relicInfoPanel) _relicInfoPanel = Instantiate(_relicInfoPanelPrefab, _relicInfoPanelParent, false);
            return _relicInfoPanel;
        }

        #endregion

        // 还池前归位到本组件名下：空闲实例不长期挂在可能被销毁的单位或槽位下，下次取出再改挂
        private void ReturnToRoot(Component instance_) => instance_.transform.SetParent(transform, false);
    }
}
