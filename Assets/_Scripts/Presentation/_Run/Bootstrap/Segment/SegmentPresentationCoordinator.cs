using System;
using _DreamKeep.ConfigData;
using _DreamKeep.EngineUtility;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Presentation
{
    /// <summary>段表现协调：集中换段/收尾时的 BGM 切换、顶栏开合、结果面板与 App 层弹层转发。
    /// 只持表现引用与只读整局数据，不写域状态、不决定段流程。</summary>
    public sealed class SegmentPresentationCoordinator
    {
        private readonly UI_TopBar _topBar;
        private readonly UI_ResultPanel _resultPanel;
        private readonly UI_PausePanel _pausePanel;
        private readonly AudioRoot _audio;
        private readonly RunState _run;
        private Action _showSetting;
        private Action _showEncyclopedia;
        private Action<Action> _showGiveUp;

        public SegmentPresentationCoordinator(UI_TopBar topBar_, UI_ResultPanel resultPanel_,
            UI_PausePanel pausePanel_, AudioRoot audio_, RunState run_)
        {
            _topBar = topBar_;
            _resultPanel = resultPanel_;
            _pausePanel = pausePanel_;
            _audio = audio_;
            _run = run_;
        }

        /// <summary>补装 App 层面板入口：面板住 Scene_MainMenu，跨场景不可序列化，只能常驻方运行时转交。</summary>
        public void BindAppPanels(Action showSetting_, Action showEncyclopedia_, Action<Action> showGiveUp_)
        {
            _showSetting = showSetting_;
            _showEncyclopedia = showEncyclopedia_;
            _showGiveUp = showGiveUp_;
        }

        public void WirePausePanel(Action continue_, Action setting_, Action encyclopedia_, Action giveUp_,
            Action saveAndQuit_)
        {
            if (_pausePanel)
                _pausePanel.Wire(continue_, setting_, encyclopedia_, giveUp_, saveAndQuit_);
        }

        public void HidePausePanel()
        {
            if (_pausePanel) _pausePanel.Hide();
        }

        public void ShowSetting() => _showSetting?.Invoke();

        public void ShowEncyclopedia() => _showEncyclopedia?.Invoke();

        public void ShowGiveUp(Action confirm_) => _showGiveUp?.Invoke(confirm_);

        /// <summary>切歌：战斗段按当前地图节点判 Boss，其余段与地图共用跑图曲；同曲请求由 AudioRoot 守卫不重启。</summary>
        public void SwitchBgmOf(ESegmentKind kind_)
        {
            if (!_audio) return;
            bool battle = kind_ == ESegmentKind.Battle;
            bool boss = battle && _run.Map.CurrentNode != null
                               && _run.Map.CurrentNode.ResolvedType == EMapPointType.Boss;
            _audio.PlayBgm(boss ? _audio.Cues.BgmBoss : battle ? _audio.Cues.BgmBattle : _audio.Cues.BgmRun);
        }

        public void PlayUiClick()
        {
            if (_audio) _audio.Play(_audio.Cues.UiClick);
        }

        public void PlayBgmRun()
        {
            if (_audio) _audio.PlayBgm(_audio.Cues.BgmRun);
        }

        public void OpenMap()
        {
            if (_topBar) _topBar.OpenMap();
        }

        public void CloseMap()
        {
            if (_topBar) _topBar.CloseMap();
        }

        public void ToggleTopBar(bool active_)
        {
            if (_topBar) _topBar.gameObject.SetActive(active_);
        }

        public void HideResultPanel()
        {
            if (_resultPanel) _resultPanel.Hide();
        }

        public void ShowDefeatResult()
        {
            if (_resultPanel) _resultPanel.Show(EResultKind.RunDefeated);
        }
    }
}
