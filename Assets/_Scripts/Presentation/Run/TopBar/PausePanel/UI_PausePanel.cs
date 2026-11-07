using System;
using _DreamKeep.EngineUtility;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.Presentation
{
    /// <summary>暂停面板：继续 / 放弃 / 保存并退出三条出口转发给整局编排者，设置维持占位。</summary>
    public sealed class UI_PausePanel : UI_Panel
    {
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _settingButton;
        [SerializeField] private Button _giveUpButton;
        [SerializeField] private Button _saveAndQuitButton;

        #region 接线：三个按钮只转发意图

        public void Wire(Action onContinue_, Action onGiveUp_, Action onSaveAndQuit_)
        {
            if (_continueButton) _continueButton.onClick.AddListener(() => onContinue_?.Invoke());
            if (_giveUpButton) _giveUpButton.onClick.AddListener(() => onGiveUp_?.Invoke());
            if (_saveAndQuitButton) _saveAndQuitButton.onClick.AddListener(() => onSaveAndQuit_?.Invoke());
        }

        #endregion
    }
}
