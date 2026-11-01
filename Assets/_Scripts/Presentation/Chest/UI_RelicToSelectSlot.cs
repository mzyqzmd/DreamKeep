using _DreamKeep.CommonPresentation;
using _DreamKeep.ConfigData;
using _DreamKeep.EngineUtility;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.Presentation
{
    /// <summary>单件待选遗物槽位：图标来自 SO_Relic（写根 Image），点击只上报不自行动（继承自 UI_Button）。</summary>
    [RequireComponent(typeof(UI_HoverTip))]
    public sealed class UI_RelicToSelectSlot : UI_Button
    {
        [SerializeField] private Image _icon;
        private UI_HoverTip _hoverTip;

        private void Awake() => _hoverTip = GetComponent<UI_HoverTip>();

        public void Bind(SO_Relic relic_)
        {
            if (relic_ == null) return;
            gameObject.SetActive(true);
            _icon.sprite = relic_.Icon;
            _hoverTip.SetContents(relic_.RelicName, relic_.Description);
        }

        /// <summary>领取后锁死点击，避免重复触发。</summary>
        public void Lock()
        {
            Enabled = false;
            gameObject.SetActive(false);
        }
    }
}