using System;
using _DreamKeep.CommonPresentation;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;
using _DreamKeep.EngineUtility;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Presentation
{
    /// <summary>常驻主菜单编排者（住 Scene_MainMenu 的 GameRoot）：负责主菜单与整局之间的进出场协调。
    /// 弹层互斥交给 AppOverlayCoordinator、场景装卸与容器构建交给 RunSceneGateway，本类只编排进出局流程与存档时序。</summary>
    public sealed class MainMenuFlow : MonoBehaviour
    {
        [Tooltip("黑屏过渡幕：进局/回主菜单全程盖住画面（同场景常驻，Inspector 拖引用）")] [SerializeField]
        private UI_FadeScreen _fadeScreen;

        [Tooltip("常驻主相机：Run 内 Screen Space-Camera 画布共用它（同场景，Inspector 拖引用）")] [SerializeField]
        private Camera _camera;

        [Tooltip("主菜单面板：进局后收起、回主菜单后亮出（同场景，Inspector 拖引用）")] [SerializeField]
        private UI_MainMenuPanel _mainMenuPanel;

        [Tooltip("设置面板：主菜单与整局内共用（同场景 Canvas_System，默认失活）")] [SerializeField]
        private UI_SettingPanel _settingPanel;

        [Tooltip("百科大全面板（同场景 Canvas_System，默认失活）")] [SerializeField]
        private UI_EncyclopediaPanel _encyclopediaPanel;

        [Tooltip("放弃二次确认面板：主菜单删档与整局内放弃共用，确认语义由调用方给定（同场景 Canvas_System，默认失活）")] [SerializeField]
        private UI_GiveUpPanel _giveUpPanel;

        [Tooltip("开发日志面板（同场景 Canvas_System，默认失活）")] [SerializeField]
        private UI_DevelopLogPanel _developLogPanel;

        [Tooltip("退出确认面板（同场景 Canvas_System，默认失活）")] [SerializeField]
        private UI_QuitGamePanel _quitGamePanel;

        [Tooltip("整局跑图基地场景名：additive 加载与卸载")] [SerializeField]
        private string _runSceneName = "Scene_Run";

        private Func<ESegmentKind, UniTask> _startSegment; // 进段入口（任务在段装配完成后结束）：整局容器 Build 回调里注入
        private Func<UniTask> _prepareReturnToMenu; // 回主菜单前的 Run 侧清理（归位挂起态+关会话+卸段）
        private bool _switching; // 进出过渡闸门：黑屏期间不接新入口
        private bool _inRun; // 是否处于整局内：决定进局/回主菜单各自是否可用
        private AudioRoot _audio; // 常驻音频根：住本对象下的 SFXRoot，不进容器，Awake 自愈
        private ScreenShakeDriver _shakeDriver; // 常驻抖动驱动：挂在主相机上，Build 整局容器前转交
        private AppOverlayCoordinator _overlays;
        private RunSceneGateway _gateway;

        /// <summary>常驻主相机：Run 内 Screen Space-Camera 画布与段装卸器共用。</summary>
        public Camera MainCamera => _camera;

        /// <summary>常驻黑屏过渡幕：唯一实例，进局时经容器转交段编排者，换段/卸段共用同一份。</summary>
        public UI_FadeScreen FadeScreen => _fadeScreen;

        /// <summary>注入段编排者入口：由整局容器（Presentation 程序集）Build 回调调用，委托保依赖方向。</summary>
        public void BindRunHandlers(Func<ESegmentKind, UniTask> startSegment_, Func<UniTask> prepareReturnToMenu_)
        {
            _startSegment = startSegment_;
            _prepareReturnToMenu = prepareReturnToMenu_;
        }

        /// <summary>Run 侧回主菜单请求上报（整局终点确认）：黑屏收尾并卸载整局场景。</summary>
        public void RequestReturnToMainMenu()
        {
            if (_switching || !_inRun) return;
            _switching = true;
            ReturnToMenuAsync().Forget();
        }

        private void Awake()
        {
            // 本组件挂 GameRoot（常驻激活）：面板默认失活也不影响绑定，序列化引用不依赖面板 Awake；
            // 偏好恢复先于一切：音量系数写回工具层，分辨率与全屏按落盘值应用一次
            GameSettings.ApplyOnBoot();
            _audio = GetComponentInChildren<AudioRoot>(true);
            if (_audio) _audio.PlayBgm(_audio.Cues.BgmMainMenu);
            // 抖动驱动挂在常驻主相机上，不在本对象子节点里，只能按相机取
            _shakeDriver = _camera ? _camera.GetComponent<ScreenShakeDriver>() : null;
            // 全局点击音：常驻 EventSystem 上的唯一拦截器，音频根在此转交（主菜单阶段整局容器尚未构建）
            UI_ClickSfx clickSfx = GetComponentInChildren<UI_ClickSfx>(true);
            if (clickSfx) clickSfx.Bind(_audio);

            _overlays = new AppOverlayCoordinator(_mainMenuPanel, _settingPanel, _encyclopediaPanel,
                _giveUpPanel, _developLogPanel, _quitGamePanel);
            _gateway = new RunSceneGateway(_runSceneName, _camera, _audio, _shakeDriver, nameof(MainMenuFlow));

            _overlays.WireMainMenu(OnNewGameClicked, OnContinueClicked, OnGiveUpSaveClicked,
                ShowSettingPanel, ShowEncyclopediaPanel, ShowDevelopLogPanel, ShowQuitGamePanel);
            _overlays.SetHasSave(SaveService.HasSave); // 启动时按磁盘上的单档切两态

            _overlays.WireGiveUp(_overlays.CloseGiveUp, _overlays.ConfirmGiveUp);
            _overlays.WireQuit(_overlays.CloseQuit, OnQuitConfirmed);
            // 主菜单阶段没有整局容器：百科三页的弹层工厂依赖在这里显式装配，进局后由整局容器构建回调再补一次
            _overlays.ConstructEncyclopedia(GetComponent<PopFactory>());

            _overlays.HideOverlays(); // 常驻场景：进 Play 先统一收一遍，避免 Inspector 残留激活态
        }

        private void OnDestroy()
        {
            _startSegment = null;
            _prepareReturnToMenu = null;
            _overlays?.ClearCallbacks();
        }

        #region 主菜单按钮出口：新建 / 继续 / 放弃存档

        private void OnNewGameClicked()
        {
            if (_switching || _inRun) return;
            SaveService.ClearRestoreIntent(); // 新局一律走常规构造，不接任何残留的待读快照
            _switching = true;
            EnterRunAsync().Forget();
        }

        // 继续：先把单档读进静态待读快照，再走同一条黑屏进局链；坏档按无档处理并切回两态
        private void OnContinueClicked()
        {
            if (_switching || _inRun) return;
            if (!SaveService.Load())
            {
                _overlays.SetHasSave(false);
                return;
            }

            _switching = true;
            EnterRunAsync().Forget();
        }

        // 主菜单的放弃＝删档：确认后留在主菜单并切回无档态
        private void OnGiveUpSaveClicked() => _overlays.ShowGiveUp(() =>
        {
            SaveService.Delete();
            _overlays.SetHasSave(false);
        });

        private void ShowDevelopLogPanel() => _overlays.ShowDevelopLog();

        /// <summary>打开百科大全：主菜单按钮与整局暂停面板共用同一份常驻面板；开前先收起其余弹层。</summary>
        public void ShowEncyclopediaPanel() => _overlays.ShowEncyclopedia();

        #endregion

        #region 常驻弹层面板：主菜单与整局共用同一份，开一个先收其余

        /// <summary>打开设置面板：主菜单按钮与整局暂停面板共用；叠在当前层之上，不关底下面板。</summary>
        public void ShowSettingPanel() => _overlays.ShowSetting();

        /// <summary>打开放弃确认：confirm_ 是「确认放弃」的当前语义（主菜单=删档、整局内=结束本局）。</summary>
        public void ShowGiveUpPanel(Action confirm_) => _overlays.ShowGiveUp(confirm_);

        #endregion

        #region 退出应用：确认 → 渐入黑屏 → 退出

        private void ShowQuitGamePanel() => _overlays.ShowQuit();

        private void OnQuitConfirmed()
        {
            if (_overlays.Quitting) return;
            _overlays.Quitting = true;
            QuitAsync().Forget();
        }

        // 退出前只淡入不揭幕：黑幕盖住后直接结束进程（编辑器下停 Play）
        private async UniTask QuitAsync()
        {
            if (_fadeScreen) await _fadeScreen.CoverStayAsync();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region 进局：黑屏 → 装载整局场景并手动 Build → 藏主菜单 → 进先古段并等装配完成 → 揭幕

        private async UniTask EnterRunAsync()
        {
            try
            {
                await _fadeScreen.CoverAsync(RunEnterCoreAsync);
            }
            finally
            {
                _switching = false; // 揭幕后再放行：黑屏淡出期间不接新入口
            }
        }

        // 黑屏内的进局链：任一步失败都停留主菜单（揭幕即主菜单原样），不留半装载的整局场景
        private async UniTask RunEnterCoreAsync()
        {
            if (!await _gateway.LoadAsync(destroyCancellationToken))
            {
                Debug.LogError($"[{nameof(MainMenuFlow)}] 整局场景 {_runSceneName} 加载失败，留在主菜单");
                await _gateway.UnloadAsync(); // 场景取到但缺 scope 时句柄仍有效，回收掉避免残留
                return;
            }

            _gateway.BuildScope();
            _overlays.HideMainMenu();
            _inRun = true;
            if (_audio) _audio.PlayBgm(_audio.Cues.BgmRun);
            // 新局以先古之民起手；读档按存档当前节点类型重进该段，段装配完成后外层才揭幕
            int restoredKind = SaveService.ConsumeRestoreSegmentKind();
            ESegmentKind first = restoredKind >= 0 ? (ESegmentKind)restoredKind : ESegmentKind.Ancient;
            if (_startSegment == null) Debug.LogError($"[{nameof(MainMenuFlow)}] 整局容器未注入进段入口，首段不会加载");
            else await _startSegment(first);
        }

        #endregion

        #region 回主菜单：黑屏 → Run 侧清理 → 卸载整局场景 → 亮出主菜单 → 揭幕

        private async UniTask ReturnToMenuAsync()
        {
            try
            {
                await _fadeScreen.CoverAsync(ReturnToMenuCoreAsync);
            }
            finally
            {
                _switching = false; // 揭幕后再放行：黑屏淡出期间不接新入口
            }
        }

        // 黑屏内的回主菜单链：Run 侧先清完（段编排者随场景卸载自毁），再卸整局场景、亮主菜单
        private async UniTask ReturnToMenuCoreAsync()
        {
            if (_prepareReturnToMenu != null) await _prepareReturnToMenu();
            await _gateway.UnloadAsync();
            SaveService.Unbind(); // 整局场景已卸载，静态门面不再持有随场景销毁的对象集合
            _overlays.HideOverlays(); // 整局内开过的常驻弹层（设置/放弃）随回菜单一并收起
            _overlays.ShowMainMenu();
            _overlays.SetHasSave(SaveService.HasSave); // 保存并退出留档、终点与放弃删档，两态按磁盘真值刷新

            _inRun = false;
            if (_audio) _audio.PlayBgm(_audio.Cues.BgmMainMenu);
        }

        #endregion
    }
}
