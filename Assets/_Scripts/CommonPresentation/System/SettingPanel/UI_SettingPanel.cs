using System.Collections.Generic;
using TMPro;
using _DreamKeep.EngineUtility;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>设置面板（常驻主菜单画布，主菜单与整局内共用）：BGM/音效双滑条、分辨率下拉、全屏开关，
    /// 控件值一律读写 <see cref="GameSettings"/>（应用级偏好，与存档无关）。
    /// 面板默认失活、Awake 不会跑，接线与回填都放在首次 OnShown 里做。</summary>
    public sealed class UI_SettingPanel : UI_Panel
    {
        [SerializeField] private Slider _BgmSlider;
        [SerializeField] private Slider _SfxSlider;
        [SerializeField] private TMP_Dropdown _resolutionDropdown;
        [SerializeField] private Toggle _fullScreenToggle;

        [Tooltip("全屏开关的勾选图标：按开关状态在两张 sprite 之间切换")] [SerializeField]
        private Image _fullScreenCheckmark;

        [SerializeField] private Sprite _fullScreenTickedSprite;
        [SerializeField] private Sprite _fullScreenUnTickedSprite;

        [SerializeField] private Button _closeButton;

        private bool _wired;

        protected override void OnShown()
        {
            WireOnce();
            RefreshControls();
            RefreshFullScreenIcon();
        }

        #region 接线与回填：首次显示接线，之后每次显示按当前偏好回填

        private void WireOnce()
        {
            if (_wired) return;
            _wired = true;
            if (_BgmSlider) _BgmSlider.onValueChanged.AddListener(GameSettings.SetBgmVolume);
            if (_SfxSlider) _SfxSlider.onValueChanged.AddListener(GameSettings.SetSfxVolume);
            if (_fullScreenToggle) _fullScreenToggle.onValueChanged.AddListener(OnFullScreenChanged);
            if (_resolutionDropdown) _resolutionDropdown.onValueChanged.AddListener(GameSettings.SetResolutionIndex);
            if (_closeButton) _closeButton.onClick.AddListener(Hide);
        }

        // 回填一律 SetValueWithoutNotify：避免把"刷新"误当成玩家操作再写一遍偏好
        private void RefreshControls()
        {
            if (_BgmSlider) _BgmSlider.SetValueWithoutNotify(GameSettings.BgmVolume);
            if (_SfxSlider) _SfxSlider.SetValueWithoutNotify(GameSettings.SfxVolume);
            if (_fullScreenToggle) _fullScreenToggle.SetIsOnWithoutNotify(GameSettings.Fullscreen);
            if (!_resolutionDropdown) return;

            _resolutionDropdown.ClearOptions();
            _resolutionDropdown.AddOptions(new List<string>(GameSettings.ResolutionLabels));
            _resolutionDropdown.SetValueWithoutNotify(GameSettings.ResolutionIndex);
            _resolutionDropdown.RefreshShownValue();
        }

        private void OnFullScreenChanged(bool value_)
        {
            GameSettings.SetFullscreen(value_);
            RefreshFullScreenIcon();
        }

        // 勾选态换图标：勾上用 ticked、未勾用 unticked，缺 sprite 时保留原图不覆盖
        private void RefreshFullScreenIcon()
        {
            if (!_fullScreenCheckmark) return;
            Sprite sprite = GameSettings.Fullscreen ? _fullScreenTickedSprite : _fullScreenUnTickedSprite;
            if (sprite) _fullScreenCheckmark.sprite = sprite;
        }

        #endregion
    }
}