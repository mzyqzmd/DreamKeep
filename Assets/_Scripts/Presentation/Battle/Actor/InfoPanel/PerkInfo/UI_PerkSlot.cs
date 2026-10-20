using _DreamKeep.RuntimeData;
using _DreamKeep.CommonPresentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.Presentation
{
    /// <summary>状态槽位：PerkGroup 里的一格，显示状态的图标与当前层数（恒显示含 1 层）；
    /// 图标与层数由 Bind 写入，悬停说明交给同节点的气泡发起器（内容在这里喂给它，触发方式与定位都在那份组件上配）。</summary>
    [RequireComponent(typeof(UI_HoverTip))]
    public sealed class UI_PerkSlot : MonoBehaviour
    {
        [SerializeField] private Image _perkIcon;
        [SerializeField] private TextMeshProUGUI _perkAmount;

        /// <summary>绑定的挂载实例：图标、层数与悬停气泡内容都取自它。</summary>
        public BattlePerk Perk { get; private set; }

        private UI_HoverTip _hoverTip;

        // 同节点的气泡发起器由 RequireComponent 保证存在，取出即用（预制体零额外接线）
        private void Awake() => _hoverTip = GetComponent<UI_HoverTip>();

        #region 对外装配与刷新：写入状态数据，并把悬停说明喂给同节点的气泡发起器

        public void Bind(BattlePerk perk_)
        {
            Perk = perk_;
            _perkIcon.sprite = perk_.Icon;
            _perkAmount.text = perk_.Layers.ToString();
            _hoverTip.SetContents(perk_.Name, perk_.GetDescription());
        }

        // 层数刷新：域侧递减后由宿主调用，恒显示当前层数（含 1 层）；描述内嵌的层数已变，正悬停时气泡随之重刷
        public void RefreshLayers()
        {
            _perkAmount.text = Perk.Layers.ToString();
            _hoverTip.SetContents(Perk.Name, Perk.GetDescription());
        }

        #endregion
    }
}
