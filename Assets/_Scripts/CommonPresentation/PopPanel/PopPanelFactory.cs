using UnityEngine;
using VContainer;
using VContainer.Unity; // InjectGameObject 扩展方法住在这里

namespace _DreamKeep.CommonPresentation
{
    /// <summary>全屏交互面板工厂：挂在场景 GameRoot 下，统一懒建并缓存奖励面板、卡牌三选一面板与卡牌移除面板三个常驻交互面板——
    /// 面板由本工厂在容器外实例化后补一次注入（卡上的悬停气泡要靠 PopFactory，由容器内注册解析），并先关掉免得闪一帧；
    /// 内容与退场都归面板与调用方，本工厂只负责造、存放与停泊父级切换。
    /// 面板一律挂进画布才渲染：本工厂不在 Canvas 子树下，段场景面板由段装配显式停泊；_panelParent 只是 run 级默认停泊位
    /// （主菜单自己的画布），供 SegmentSceneManager 的无参取用与面板被段卸载带走后的兜底重建。</summary>
    public sealed class PopPanelFactory : MonoBehaviour
    {
        [SerializeField] private UI_RewardPanel _rewardPanelPrefab;
        [SerializeField] private UI_CardOneInThreePanel _cardChoicePanelPrefab;
        [SerializeField] private UI_CardRemovePanel _cardRemovePanelPrefab;
        [Tooltip("run 级默认停泊位（主菜单自己的画布）：仅兜底首次创建未给父级的取用，段场景面板由段装配显式停泊")] [SerializeField]
        private RectTransform _panelParent;

        private UI_RewardPanel _rewardPanel;
        private UI_CardOneInThreePanel _cardChoicePanel;
        private UI_CardRemovePanel _cardRemovePanel;
        private IObjectResolver _resolver;

        [Inject]
        public void Construct(IObjectResolver resolver_) => _resolver = resolver_;

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

        /// <summary>收起已建出的奖励面板（未建过则什么都不做）：段切换前的收场兜底用——
        /// 不得走 AcquireRewardPanel().Close()，那会为"关一次"凭空懒建出一个面板克隆体。</summary>
        public void TryCloseRewardPanel()
        {
            if (_rewardPanel) _rewardPanel.Close();
        }

        /// <summary>收起已建出的卡牌三选一面板（未建过则什么都不做）：同上，收场禁用懒建取用。</summary>
        public void TryCloseCardChoicePanel()
        {
            if (_cardChoicePanel) _cardChoicePanel.Close();
        }

        /// <summary>取卡牌移除面板单例（首次访问才实例化）：玩家从卡组选出指定张数，移除与删牌演出归面板自己，
        /// 结果经 WaitResultAsync 交付；停泊父级语义同奖励面板。</summary>
        public UI_CardRemovePanel AcquireCardRemovePanel(RectTransform parent_ = null)
        {
            if (!_cardRemovePanel) _cardRemovePanel = CreatePanel(_cardRemovePanelPrefab, parent_);
            else Relocate(_cardRemovePanel.transform, parent_);
            return _cardRemovePanel;
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
