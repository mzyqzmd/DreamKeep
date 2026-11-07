using System.Collections.Generic;
using _DreamKeep.EngineUtility;
using UnityEngine;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>通用临时弹层工厂：挂在场景 GameRoot 下，统一持有说明气泡、数字飘字与对话气泡的实例池，以及横幅公告——
    /// 说明气泡的"实例取还与唯一列守卫"归此处（同一时刻只许一列、谁的归还只收自己那列），叠列定位由消费方 UI_HoverTip 接管；
    /// 数字飘字的父节点与世界坐标由调用方（如战斗弹层编排）算好传入，本类不认识 Creature / UI_ActorRoot / 任何玩法事件；
    /// 对话气泡按朝向分左右两池，同侧播放中直接丢弃新请求（不排队、不叠播），父节点与锚点同样由调用方给；
    /// 横幅公告同理只由本类造与缓存单例（懒建、重播由横幅自身负责），播完即走、无交互。
    /// 全屏交互面板（奖励面板 / 卡牌三选一面板）已拆到 PopPanelFactory。
    /// 弹层一律挂到"所属对象"之下；归还只失活、不归位父级——下次取出再改挂（归还常发生在宿主失活回调里，此时改层级是非法的）。</summary>
    public sealed class PopFactory : MonoBehaviour
    {
        [SerializeField] private UI_DescriptionPopTip _descriptionPopTipPrefab;
        [SerializeField] private UI_NumberPopTip _numberPopTipPrefab;
        [SerializeField] private UI_PopBanner _bannerPrefab;
        [Header("对话气泡")] [SerializeField] private UI_SpeechBubble _speechBubbleLeftPrefab;
        [SerializeField] private UI_SpeechBubble _speechBubbleRightPrefab;

        private MonoObjectPool<UI_DescriptionPopTip> _tipPool;
        private MonoObjectPool<UI_NumberPopTip> _numberPopTipPool;
        private MonoObjectPool<UI_SpeechBubble> _speechLeftPool;
        private MonoObjectPool<UI_SpeechBubble> _speechRightPool;
        private UI_PopBanner _banner;
        private UI_SpeechBubble _speechLeft; // 同侧唯一：播放中不再取新实例
        private UI_SpeechBubble _speechRight;
        private readonly List<UI_DescriptionPopTip> _shownTips = new(); // 当前这一列的展示名单（按叠列顺序）：归还只认它
        private object _tipsRequester;

        private void Awake()
        {
            // 归还只由池内失活完成：气泡与飘字常随宿主失活一起收回，那一刻改父级会被 Unity 拒绝并中断归还；
            // 空闲实例留在原宿主名下无妨——下次取出即改挂到新位置，宿主被销毁留下的伪空由池自己跳过
            _tipPool = new MonoObjectPool<UI_DescriptionPopTip>(transform);
            _numberPopTipPool = new MonoObjectPool<UI_NumberPopTip>(transform, onReturn_: tip_ => tip_.ResetView());
            _speechLeftPool = new MonoObjectPool<UI_SpeechBubble>(transform, onReturn_: bubble_ => bubble_.ResetView());
            _speechRightPool = new MonoObjectPool<UI_SpeechBubble>(transform, onReturn_: bubble_ => bubble_.ResetView());
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

        /// <summary>在指定父节点下、以给定世界坐标播放一条飘字；播完还池。</summary>
        public void SpawnNumber(string text_, Color color_, ENumberPopTipType type_, Transform parent_,
            Vector3 worldPosition_)
        {
            UI_NumberPopTip tip = _numberPopTipPool.Get(_numberPopTipPrefab);
            tip.transform.SetParent(parent_, false);
            tip.transform.SetAsLastSibling();
            tip.Play(text_, color_, type_, worldPosition_, () => _numberPopTipPool.Release(tip));
        }

        #endregion

        #region 对话气泡：按朝向分池，同侧播放中直接丢弃新请求

        /// <summary>在指定父节点下、以给定世界坐标播放一句台词；同侧已有气泡在播时返回 false 且不排队。
        /// 空台词直接丢弃，实例播完自动还池。</summary>
        public bool Speak(ESpeechBubbleSide side_, Transform parent_, Vector3 worldPosition_, string text_)
        {
            if (string.IsNullOrEmpty(text_)) return false;
            UI_SpeechBubble current = side_ == ESpeechBubbleSide.Left ? _speechLeft : _speechRight;
            if (current != null && current.IsPlaying) return false;

            MonoObjectPool<UI_SpeechBubble> pool = side_ == ESpeechBubbleSide.Left ? _speechLeftPool : _speechRightPool;
            UI_SpeechBubble prefab = side_ == ESpeechBubbleSide.Left ? _speechBubbleLeftPrefab : _speechBubbleRightPrefab;
            UI_SpeechBubble bubble = pool.Get(prefab);
            bubble.transform.SetParent(parent_, false);
            bubble.transform.SetAsLastSibling();
            ((RectTransform)bubble.transform).position = worldPosition_;
            if (side_ == ESpeechBubbleSide.Left) _speechLeft = bubble;
            else _speechRight = bubble;

            bubble.Play(text_, () =>
            {
                if (side_ == ESpeechBubbleSide.Left) _speechLeft = null;
                else _speechRight = null;
                pool.Release(bubble);
            });
            return true;
        }

        #endregion

        #region 横幅公告：父节点由调用方传入，懒实例化缓存单例、重复触发由横幅自身重播时间线

        /// <summary>在指定父节点下播放一条横幅公告（主标题 + 可选副标题）；首触发实例化并缓存单例，之后随调用方改挂父级。</summary>
        public void PopBanner(Transform parent_, string title_, string subtitle_ = null)
        {
            if (_banner == null) _banner = Instantiate(_bannerPrefab, parent_);
            else
            {
                _banner.transform.SetParent(parent_, false);
                _banner.transform.SetAsLastSibling();
            }

            _banner.Show(title_, subtitle_);
        }

        #endregion
    }
}
