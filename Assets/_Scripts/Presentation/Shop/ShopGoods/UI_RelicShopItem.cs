using _DreamKeep.CommonPresentation;
using _DreamKeep.ConfigData;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.Presentation
{
    /// <summary>商店遗物位：绑定时写图标，并把名称与描述推给同节点的气泡；点击购买沿用基类报价与被拒反馈。</summary>
    public sealed class UI_RelicShopItem : UI_ShopItem
    {
        [Tooltip("遗物图标")] [SerializeField] private Image _icon;

        [Tooltip("悬停气泡")] [SerializeField] private UI_HoverTip _hoverTip;

        /// <summary>本格上架的遗物：成交时按它入遗物栏。</summary>
        public SO_Relic Relic { get; private set; }

        private void Awake() => EnsureRefs();

        #region 绑定：写图标与气泡，再写价

        public void Bind(SO_Relic relic_, int price_, int gold_)
        {
            EnsureRefs();
            Relic = relic_;
            _icon.sprite = relic_.Icon;
            _hoverTip.SetContents(relic_.RelicName, relic_.Description);
            ShowPrice(price_, gold_);
        }

        #endregion

        // 引用自愈：格子可能在隐藏的地毯里被绑定（Awake 未跑），气泡按同节点补取一次；
        // 图标是序列化引用（_icon），不再按类型猜第一个 Image——猜错会写到槽位底板
        private void EnsureRefs()
        {
            if (!_hoverTip) _hoverTip = GetComponent<UI_HoverTip>();
        }
    }
}
