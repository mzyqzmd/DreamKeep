using _DreamKeep.CommonPresentation;
using _DreamKeep.ConfigData;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.Presentation
{
    /// <summary>商店药水位：绑定时写图标，并把名称与描述推给同节点的气泡；点击购买沿用基类报价与被拒反馈。
    /// 药水槽满时的失败反馈也由地毯统一调 PlayRefuse。</summary>
    public sealed class UI_PotionShopItem : UI_ShopItem
    {
        [Tooltip("药水图标")] [SerializeField] private Image _icon;

        [Tooltip("悬停气泡")] [SerializeField] private UI_HoverTip _hoverTip;

        /// <summary>本格上架的药水：成交时按它入药水槽。</summary>
        public SO_Potion Potion { get; private set; }

        private void Awake() => EnsureRefs();

        #region 绑定：写图标与气泡，再写价

        public void Bind(SO_Potion potion_, int price_, int gold_)
        {
            EnsureRefs();
            Potion = potion_;
            _icon.sprite = potion_.Icon;
            _hoverTip.SetContents(potion_.PotionName, potion_.Description);
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
