using _DreamKeep.EngineUtility;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.Presentation
{
    public sealed class UI_SettingPanel : UI_Panel
    {
        [SerializeField]private Slider _BgmSlider;
        [SerializeField]private Slider _SfxSlider;
        [SerializeField]private Dropdown _resolutionDropdown;
        [SerializeField]private Toggle _fullScreenToggle;
        [SerializeField]private Button _closeButton;
    }
}