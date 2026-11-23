using System;
using UnityEngine;
using _DreamKeep.CommonPresentation;

namespace _DreamKeep.Presentation
{
    /// <summary>常驻弹层协调：持有主菜单与整局共用的五个全屏弹层，负责互斥显隐、确认回调消费与退出闸门。
    /// 全部引用由 MainMenuFlow 从 Inspector 字段转交，本类不认识容器、场景与存档。</summary>
    public sealed class AppOverlayCoordinator
    {
        private readonly UI_MainMenuPanel _mainMenuPanel;
        private readonly UI_SettingPanel _settingPanel;
        private readonly UI_EncyclopediaPanel _encyclopediaPanel;
        private readonly UI_GiveUpPanel _giveUpPanel;
        private readonly UI_DevelopLogPanel _developLogPanel;
        private readonly UI_QuitGamePanel _quitGamePanel;

        private Action _giveUpConfirm;
        private bool _quitting;

        public AppOverlayCoordinator(UI_MainMenuPanel mainMenuPanel_, UI_SettingPanel settingPanel_,
            UI_EncyclopediaPanel encyclopediaPanel_, UI_GiveUpPanel giveUpPanel_,
            UI_DevelopLogPanel developLogPanel_, UI_QuitGamePanel quitGamePanel_)
        {
            _mainMenuPanel = mainMenuPanel_;
            _settingPanel = settingPanel_;
            _encyclopediaPanel = encyclopediaPanel_;
            _giveUpPanel = giveUpPanel_;
            _developLogPanel = developLogPanel_;
            _quitGamePanel = quitGamePanel_;
        }

        public bool Quitting
        {
            get => _quitting;
            set => _quitting = value;
        }

        public void WireMainMenu(Action newGame_, Action continue_, Action giveUpSave_,
            Action setting_, Action encyclopedia_, Action developLog_, Action quit_)
        {
            if (_mainMenuPanel)
                _mainMenuPanel.Wire(newGame_, continue_, giveUpSave_, setting_, encyclopedia_, developLog_, quit_);
        }

        public void WireGiveUp(Action closed_, Action confirmed_)
        {
            if (_giveUpPanel) _giveUpPanel.Wire(closed_, confirmed_);
        }

        public void WireQuit(Action closed_, Action confirmed_)
        {
            if (_quitGamePanel) _quitGamePanel.Wire(closed_, confirmed_);
        }

        public void SetHasSave(bool hasSave_)
        {
            if (_mainMenuPanel) _mainMenuPanel.SetHasSave(hasSave_);
        }

        public void HideMainMenu()
        {
            if (_mainMenuPanel) _mainMenuPanel.Hide();
        }

        public void ShowMainMenu()
        {
            if (_mainMenuPanel) _mainMenuPanel.Show();
        }

        public void ConstructEncyclopedia(PopFactory popFactory_)
        {
            if (!popFactory_ || !_encyclopediaPanel) return;
            _encyclopediaPanel.GetComponentInChildren<UI_CardEncyclopediaPanel>(true).Construct(popFactory_);
            _encyclopediaPanel.GetComponentInChildren<UI_RelicEncyclopediaPanel>(true).Construct(popFactory_);
            _encyclopediaPanel.GetComponentInChildren<UI_PotionEncyclopediaPanel>(true).Construct(popFactory_);
        }

        #region 五弹层互斥：开任一个先收其余

        public void ShowDevelopLog()
        {
            HideOverlays();
            if (_developLogPanel) _developLogPanel.Show();
        }

        public void ShowEncyclopedia()
        {
            HideOverlays();
            if (_encyclopediaPanel) _encyclopediaPanel.Show();
        }

        public void ShowSetting()
        {
            HideOverlays();
            if (_settingPanel) _settingPanel.Show();
        }

        public void ShowQuit()
        {
            HideOverlays();
            if (_quitGamePanel) _quitGamePanel.Show();
        }

        public void ShowGiveUp(Action confirm_)
        {
            HideOverlays();
            _giveUpConfirm = confirm_;
            if (_giveUpPanel) _giveUpPanel.Show();
        }

        /// <summary>五弹层互斥：开任一个先把其余收起，避免两层全屏遮罩叠着互相吃点击。</summary>
        public void HideOverlays()
        {
            _giveUpConfirm = null;
            if (_settingPanel) _settingPanel.Hide();
            if (_giveUpPanel) _giveUpPanel.Hide();
            if (_developLogPanel) _developLogPanel.Hide();
            if (_quitGamePanel) _quitGamePanel.Hide();
            if (_encyclopediaPanel) _encyclopediaPanel.Hide();
        }

        public void CloseGiveUp()
        {
            _giveUpConfirm = null;
            if (_giveUpPanel) _giveUpPanel.Hide();
        }

        /// <summary>确认即消费回调：面板收起后再触发，避免回调里再次开面板（整局内放弃链会弹结算面板）。</summary>
        public void ConfirmGiveUp()
        {
            Action confirm = _giveUpConfirm;
            if (_giveUpPanel) _giveUpPanel.Hide();
            _giveUpConfirm = null;
            confirm?.Invoke();
        }

        public void CloseQuit()
        {
            if (_quitGamePanel) _quitGamePanel.Hide();
        }

        public void ClearCallbacks()
        {
            _giveUpConfirm = null;
        }

        #endregion
    }
}
