using System.Collections.Generic;
using _DreamKeep.EngineUtility;
using UnityEngine;
using VContainer;
using VContainer.Unity; // InjectGameObject 扩展方法住在这里

namespace _DreamKeep.CommonPresentation
{
    /// <summary>通用弹层工厂：挂在场景 GameRoot 下，统一持有说明气泡与数字飘字的实例池——
    /// 说明气泡的"实例取还与唯一列守卫"归此处（同一时刻只许一列、谁的归还只收自己那列），叠列定位由消费方 UI_HoverTip 接管；
    /// 数字飘字的父节点与世界坐标由调用方（如战斗弹层编排）算好传入，本类不认识 Creature / UI_ActorRoot / 任何玩法事件；
    /// 奖励面板与卡牌三选一面板同理只由本类造与存放（懒建单例），写什么内容、什么时候收起都归面板与调用方。
    /// 弹层一律挂到"所属对象"之下；归还只失活、不归位父级——下次取出再改挂（归还常发生在宿主失活回调里，此时改层级是非法的）。</summary>
    public sealed class PopFactory : MonoBehaviour
    {
        [SerializeField] private UI_DescriptionPopTip _descriptionPopTipPrefab;
        [SerializeField] private UI_NumberPopTip _numberPopTipPrefab;
        [SerializeField] private UI_PopBanner _bannerPrefab;
        [SerializeField] private UI_RewardPanel _rewardPanelPrefab;
        [SerializeField] private UI_CardOneInThreePanel _cardChoicePanelPrefab;
        [Tooltip("全屏面板的挂载父级：本工厂不在 Canvas 子树下，面板必须挂进画布才渲染")] [SerializeField]
        private RectTransform _panelParent;

        private MonoObjectPool<UI_DescriptionPopTip> _tipPool;
        private MonoObjectPool<UI_NumberPopTip> _numberPopTipPool;
        private UI_PopBanner _banner;
        private UI_RewardPanel _rewardPanel;
        private UI_CardOneInThreePanel _cardChoicePanel;
        private IObjectResolver _resolver;
        private readonly List<UI_DescriptionPopTip> _shownTips = new(); // 当前这一列的展示名单（按叠列顺序）：归还只认它
        private object _tipsRequester;

        private void Awake()
        {
            // 归还只由池内失活完成：气泡与飘字常随宿主失活一起收回，那一刻改父级会被 Unity 拒绝并中断归还；
            // 空闲实例留在原宿主名下无妨——下次取出即改挂到新位置，宿主被销毁留下的伪空由池自己跳过
            _tipPool = new MonoObjectPool<UI_DescriptionPopTip>(transform);
            _numberPopTipPool = new MonoObjectPool<UI_NumberPopTip>(transform, onReturn_: tip_ => tip_.ResetView());
        }

        [Inject]
        public void Construct(IObjectResolver resolver_) => _resolver = resolver_;

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

        #region 全屏面板：懒建单例，内容与退场都归调用方

        /// <summary>取奖励面板单例（首次访问才实例化）：奖励清单与"继续"回调由调用方 Show 时传入，
        /// 条目长什么样、点了怎么兑现由面板自己负责。<paramref name="parent_"/> 是停泊父级——
        /// 传了且与现挂不符才换挂（段场景卸载会带走停泊在里面的面板，重建/换挂由段的装配负责）；
        /// 不传即维持原位，后续取出不把已停泊的面板拉回默认画布。</summary>
        public UI_RewardPanel AcquireRewardPanel(RectTransform parent_ = null)
        {
            if (!_rewardPanel) _rewardPanel = CreatePanel(_rewardPanelPrefab, parent_);
            else Relocate(_rewardPanel.transform, parent_);
            return _rewardPanel;
        }

        /// <summary>取卡牌三选一面板单例（首次访问才实例化）：给哪几张牌、选中后做什么也由调用方传入；
        /// 停泊父级语义同奖励面板——三选一通常跟随奖励面板所在画布。</summary>
        public UI_CardOneInThreePanel AcquireCardChoicePanel(RectTransform parent_ = null)
        {
            if (!_cardChoicePanel) _cardChoicePanel = CreatePanel(_cardChoicePanelPrefab, parent_);
            else Relocate(_cardChoicePanel.transform, parent_);
            return _cardChoicePanel;
        }

        #endregion

        // 面板是容器外生成的场景组件：补一次注入（卡上的悬停气泡要靠它拿 PopFactory），并先关掉免得闪一帧
        private T CreatePanel<T>(T prefab_, RectTransform parent_) where T : Component
        {
            T panel = Instantiate(prefab_, parent_ ? parent_ : _panelParent);
            _resolver.InjectGameObject(panel.gameObject);
            panel.gameObject.SetActive(false);
            return panel;
        }

        // 缓存实例换挂：仅在显式给父级且父级不符时执行（null=不换挂，停泊归段的装配负责）
        private void Relocate(Transform panel_, RectTransform parent_)
        {
            if (!parent_) return;
            if (panel_.parent == parent_) return;
            panel_.SetParent(parent_, false);
        }
    }
}
