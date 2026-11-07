using System;
using _DreamKeep.EngineUtility;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.Presentation
{
    /// <summary>主菜单：只管按钮的点击转发，回调由常驻主菜单编排者给出，本面板不认识任何服务。</summary>
    public sealed class UI_MainMenuPanel : UI_Panel
    {
        [SerializeField] private Button _newGameButton;
        //当前有存档时才显示放弃按钮和继续游戏按钮，并隐藏新游戏按钮（两态显隐待存档系统接入后实现）
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _giveUpButton;
        [SerializeField] private Button _settingButton;
        [SerializeField] private Button _developLogButton;
        [SerializeField] private Button _quitButton;

        #region 接线：只转发意图

        public void Wire(Action onNewGame_)
        {
            if (_newGameButton) _newGameButton.onClick.AddListener(() => onNewGame_?.Invoke());
        }

        #endregion
    }
}
