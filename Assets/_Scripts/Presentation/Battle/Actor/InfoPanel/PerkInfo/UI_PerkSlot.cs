using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _DreamKeep.Presentation
{
    /// <summary>状态槽位：PerkGroup 里的一格，显示状态的图标与当前层数（恒显示含 1 层）；
    /// 图标、层数与状态数据由 Bind 写入；悬停由本槽位自己承接，在自身顶沿上方弹该状态的名称与描述。</summary>
    public sealed class UI_PerkSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image _perkIcon;
        [SerializeField] private TextMeshProUGUI _perkAmount;

        [Tooltip("气泡底沿与本槽位顶沿的间距（画布像素）")] [SerializeField]
        private float _tipLiftPx = 1f;

        /// <summary>绑定的挂载实例：图标、层数与悬停气泡内容都取自它。</summary>
        public BattlePerk Perk { get; private set; }

        private UI_PopTextRoot _tipHost;
        private readonly List<(string Name, string Description)> _tip = new(1);
        private bool _tipShown;

        #region 对外装配与刷新：由宿主 UI_PerkGroup 注入气泡宿主并写入状态数据

        /// <summary>装配：注入说明气泡宿主（由宿主在 Instantiate 后调用）。</summary>
        public void Initialize(UI_PopTextRoot tipHost_) => _tipHost = tipHost_;

        public void Bind(BattlePerk perk_)
        {
            Perk = perk_;
            _perkIcon.sprite = perk_.Icon;
            _perkAmount.text = perk_.Layers.ToString();
        }

        // 层数刷新：域侧递减后由宿主调用，恒显示当前层数（含 1 层）；悬停中描述内嵌的层数已变，气泡同步改写
        public void RefreshLayers()
        {
            _perkAmount.text = Perk.Layers.ToString();
            if (_tipShown) ShowTip();
        }

        #endregion

        public void OnPointerEnter(PointerEventData eventData_) => ShowTip();

        public void OnPointerExit(PointerEventData eventData_) => HideTip();

        private void OnDisable() => HideTip(); // 槽位销毁/面板关激活时 Exit 不来，兜底收掉悬着的气泡

        private void ShowTip()
        {
            if (_tipHost == null || Perk == null) return;
            _tipShown = true;
            _tip.Clear();
            _tip.Add((Perk.Name, Perk.GetDescription()));
            RectTransform rect = (RectTransform)transform;
            Vector3 anchor = UiGeometry.AnchorPoint(rect, EUiAnchor.TopCenter) + rect.up * (_tipLiftPx * rect.lossyScale.y);
            _tipHost.ShowTips(this, _tip, anchor, EUiAnchor.BottomCenter);
        }

        private void HideTip()
        {
            if (!_tipShown) return;
            _tipShown = false;
            if (_tipHost) _tipHost.HideTips(this);
        }
    }
}
