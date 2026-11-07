using System;
using _DreamKeep.EngineUtility;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.Presentation
{
    /// <summary>暂停面板：继续 / 设置 / 放弃 / 保存并退出四条出口全部转发给整局编排者，本面板不认识任何服务。</summary>
    public sealed class UI_PausePanel : UI_Panel
    {
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _settingButton;
        [SerializeField] private Button _giveUpButton;
        [SerializeField] private Button _saveAndQuitButton;

        #region 接线：四个按钮只转发意图

        public void Wire(Action onContinue_, Action onSetting_, Action onGiveUp_, Action onSaveAndQuit_)
        {
            if (_continueButton) _continueButton.onClick.AddListener(() => onContinue_?.Invoke());
            if (_settingButton) _settingButton.onClick.AddListener(() => onSetting_?.Invoke());
            if (_giveUpButton) _giveUpButton.onClick.AddListener(() => onGiveUp_?.Invoke());
            if (_saveAndQuitButton) _saveAndQuitButton.onClick.AddListener(() => onSaveAndQuit_?.Invoke());
        }

        #endregion
    }
}
