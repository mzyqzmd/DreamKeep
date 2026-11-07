using System;
using _DreamKeep.EngineUtility;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>主菜单：只管按钮的点击转发，回调由常驻主菜单编排者给出，本面板不认识任何服务。
    /// 两态由 <see cref="SetHasSave"/> 切换：有档时隐藏新建、亮出继续与放弃。</summary>
    public sealed class UI_MainMenuPanel : UI_Panel
    {
        [SerializeField] private Button _newGameButton;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _giveUpButton;
        [SerializeField] private Button _settingButton;
        [SerializeField] private Button _developLogButton;
        [SerializeField] private Button _quitButton;

        #region 接线：只转发意图

        public void Wire(Action onNewGame_, Action onContinue_, Action onGiveUp_, Action onSetting_,
            Action onDevelopLog_, Action onQuit_)
        {
            if (_newGameButton) _newGameButton.onClick.AddListener(() => onNewGame_?.Invoke());
            if (_continueButton) _continueButton.onClick.AddListener(() => onContinue_?.Invoke());
            if (_giveUpButton) _giveUpButton.onClick.AddListener(() => onGiveUp_?.Invoke());
            if (_settingButton) _settingButton.onClick.AddListener(() => onSetting_?.Invoke());
            if (_developLogButton) _developLogButton.onClick.AddListener(() => onDevelopLog_?.Invoke());
            if (_quitButton) _quitButton.onClick.AddListener(() => onQuit_?.Invoke());
        }

        /// <summary>按有无存档切换两态：无档只留新建/设置/日志/退出，有档换成继续+放弃并收起新建。</summary>
        public void SetHasSave(bool hasSave_)
        {
            if (_newGameButton) _newGameButton.gameObject.SetActive(!hasSave_);
            if (_continueButton) _continueButton.gameObject.SetActive(hasSave_);
            if (_giveUpButton) _giveUpButton.gameObject.SetActive(hasSave_);
        }

        #endregion
    }
}
