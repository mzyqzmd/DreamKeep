using _DreamKeep.ConfigData;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _DreamKeep.Presentation
{
    /// <summary>状态槽位：PerkGroup 里的一格，显示状态的图标与当前层数（恒显示含 1 层），悬停时经宿主（UI_InfoPanel）弹出详情提示。
    /// 只报指针进出、不判规则，图标、层数与状态数据由 Bind 写入，宿主经 SetHost 注入。</summary>
    public class UI_PerkSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image _perkIcon;
        [SerializeField] private TextMeshProUGUI _perkAmount;

        /// <summary>绑定的挂载实例：图标、层数与弹提示内容都取自它。</summary>
        public BattlePerk Perk { get; private set; }

        private UI_InfoPanel _host;

        public void SetHost(UI_InfoPanel host_) => _host = host_;

        public void Bind(BattlePerk perk_)
        {
            Perk = perk_;
            _perkIcon.sprite = perk_.Icon;
            _perkAmount.text = perk_.Layers.ToString();
        }

        // 层数刷新：域侧递减后由宿主调用，恒显示当前层数（含 1 层）
        public void RefreshLayers() => _perkAmount.text = Perk.Layers.ToString();

        // 图标顶沿的世界坐标：详情提示的定位锚点，在它上方留出间距放 UI_DescriptionPopTip
        public Vector3 TipAnchorPosition()
        {
            RectTransform rect = _perkIcon.rectTransform;
            return rect.position + rect.up * rect.rect.height * rect.lossyScale.y * 0.5f;
        }

        public void OnPointerEnter(PointerEventData eventData_) => _host?.ShowPerkTip(this);

        public void OnPointerExit(PointerEventData eventData_) => _host?.HidePerkTip();

        // 槽位被关激活（面板收起等）时 Exit 不来，提示还悬着就漏掉了，顺手收掉
        private void OnDisable() => _host?.HidePerkTip();
    }
}