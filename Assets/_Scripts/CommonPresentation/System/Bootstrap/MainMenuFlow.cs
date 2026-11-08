using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using VContainer.Unity;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>常驻主菜单编排者（住 Scene_MainMenu 的 GameRoot）：App 侧唯一进出入口，同时是四个常驻弹层面板的唯一编排者。
    /// 进局＝黑屏 → additive 加载 Scene_Run → 关 Run 内相机/EventSystem/AudioListener、画布补常驻相机 →
    /// 手动 Build 整局容器（autoRun 关；Build 回调把段编排者入口经 BindRunHandlers 注入本类）→
    /// 藏主菜单 → 经委托进先古之民段（每把起点）并等它装配完成 → 揭幕：黑屏一直盖到段就绪，不会露出空的 Scene_Run；
    /// 回主菜单＝黑屏 → 经委托触发 Run 侧清理（关会话+卸段）→ 卸载 Scene_Run（段编排者随场景自毁）→ 亮出主菜单 → 揭幕。
    /// 主菜单四面板（设置/放弃/开发日志/退出）住本场景 Canvas_System，整局内也常驻在场，
    /// 因此暂停面板的「设置」「放弃」也经本类开，跨场景引用不落序列化。
    /// 本类在 CommonPresentation 程序集，不认识 SegmentSceneManager/RunLifetimeScope 具体类型，只拿委托。</summary>
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
        private Action _giveUpConfirm; // 放弃确认的当前语义：主菜单=删档、整局内=结束本局，由开面板的调用方给
        private Scene _runScene; // 在场的整局场景句柄：卸载与防重入用
        private bool _switching; // 进出过渡闸门：黑屏期间不接新入口
        private bool _inRun; // 是否处于整局内：决定进局/回主菜单各自是否可用
        private bool _quitting; // 退出闸门：确认后黑幕淡入期间不接二次点击

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

            if (_mainMenuPanel)
                _mainMenuPanel.Wire(OnNewGameClicked, OnContinueClicked, OnGiveUpSaveClicked, ShowSettingPanel,
                    ShowDevelopLogPanel, ShowQuitGamePanel);
            // 放弃确认：关闭即收起，确认即转发开面板时给的那份语义
            if (_giveUpPanel) _giveUpPanel.Wire(OnGiveUpClosed, OnGiveUpConfirmed);
            if (_quitGamePanel) _quitGamePanel.Wire(OnQuitClosed, OnQuitConfirmed);
            HideOverlays(); // 常驻场景：进 Play 先统一收一遍，避免 Inspector 残留激活态
        }

        private void OnDestroy()
        {
            _startSegment = null;
            _prepareReturnToMenu = null;
            _giveUpConfirm = null;
        }

        #region 主菜单按钮出口：存档相关一律占位日志，坐实接线点

        private void OnNewGameClicked()
        {
            if (_switching || _inRun) return;
            _switching = true;
            EnterRunAsync().Forget();
        }

        private void OnContinueClicked() => Debug.Log("[存档未接入] 继续游戏：从存档还原整局");

        // 主菜单的放弃＝删档：确认后留在主菜单，两态待存档接好再由 SetHasSave 切回无档
        private void OnGiveUpSaveClicked() =>
            ShowGiveUpPanel(() => Debug.Log("[存档未接入] 放弃存档：删除当前存档并切回无档态"));

        private void ShowDevelopLogPanel()
        {
            HideOverlays();
            if (_developLogPanel) _developLogPanel.Show();
        }

        #endregion

        #region 常驻弹层面板：主菜单与整局共用同一份，开一个先收其余

        /// <summary>打开设置面板：主菜单按钮与整局暂停面板共用；叠在当前层之上，不关底下面板。</summary>
        public void ShowSettingPanel()
        {
            HideOverlays();
            if (_settingPanel) _settingPanel.Show();
        }

        /// <summary>打开放弃确认：confirm_ 是「确认放弃」的当前语义（主菜单=删档、整局内=结束本局）。</summary>
        public void ShowGiveUpPanel(Action confirm_)
        {
            HideOverlays();
            _giveUpConfirm = confirm_;
            if (_giveUpPanel) _giveUpPanel.Show();
        }

        // 四个弹层互斥：开任一个先把其余收起，避免两层全屏遮罩叠着互相吃点击
        private void HideOverlays()
        {
            _giveUpConfirm = null;
            if (_settingPanel) _settingPanel.Hide();
            if (_giveUpPanel) _giveUpPanel.Hide();
            if (_developLogPanel) _developLogPanel.Hide();
            if (_quitGamePanel) _quitGamePanel.Hide();
        }

        private void OnGiveUpClosed()
        {
            _giveUpConfirm = null;
            if (_giveUpPanel) _giveUpPanel.Hide();
        }

        // 确认即消费掉回调：面板收起后再触发，避免回调里再次开面板（整局内的放弃链会弹结算面板）
        private void OnGiveUpConfirmed()
        {
            Action confirm = _giveUpConfirm;
            if (_giveUpPanel) _giveUpPanel.Hide();
            _giveUpConfirm = null;
            confirm?.Invoke();
        }

        #endregion

        #region 退出应用：确认 → 渐入黑屏 → 退出

        private void ShowQuitGamePanel()
        {
            HideOverlays();
            if (_quitGamePanel) _quitGamePanel.Show();
        }

        private void OnQuitClosed()
        {
            if (_quitGamePanel) _quitGamePanel.Hide();
        }

        private void OnQuitConfirmed()
        {
            if (_quitting) return;
            _quitting = true;
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
            if (!await LoadRunSceneAsync())
            {
                Debug.LogError($"[{nameof(MainMenuFlow)}] 整局场景 {_runSceneName} 加载失败，留在主菜单");
                return;
            }

            BuildRunScope();
            if (_mainMenuPanel) _mainMenuPanel.Hide();
            _inRun = true;
            // 先古之民是每一把游戏的起点，段收尾自动开地图；等它装配完成后外层才揭幕，黑屏不提前露出空的整局场景
            if (_startSegment == null) Debug.LogError($"[{nameof(MainMenuFlow)}] 整局容器未注入进段入口，首段不会加载");
            else await _startSegment(ESegmentKind.Ancient);
        }

        // 仿 SegmentSceneLoader 范式：待激活态加载 → 临时接管输入 → 关 Run 内相机/EventSystem/AudioListener、
        // 画布补常驻相机 → 找 autoRun 关闭的整局 scope（不 Build，交给调用方在委托注入后手动 Build）
        private async UniTask<bool> LoadRunSceneAsync()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(_runSceneName, LoadSceneMode.Additive);
            if (load == null)
            {
                Debug.LogError($"[{nameof(MainMenuFlow)}] 场景 {_runSceneName} 不在构建列表，无法进局");
                return false;
            }

            load.allowSceneActivation = false;
            await UniTask.WaitUntil(() => load.progress >= 0.9f);

            // 激活前临时关常驻 EventSystem（此刻全局列表为空），激活后再关 Run 内的、最后恢复常驻：
            // 全程至多一个 active EventSystem，从根上避免 "There can be only one active Event System"
            EventSystem resident = EventSystem.current;
            if (resident) resident.enabled = false;

            try
            {
                load.allowSceneActivation = true;
                await load.ToUniTask();

                Scene newScene = SceneManager.GetSceneByName(_runSceneName);
                if (!newScene.IsValid())
                {
                    Debug.LogError($"[{nameof(MainMenuFlow)}] 场景 {_runSceneName} 激活后取不到句柄");
                    return false;
                }

                _runScene = newScene;

                // 关掉 Run 自带相机 / EventSystem / AudioListener（组件级，资产仍保留供编辑查看）
                foreach (GameObject root in newScene.GetRootGameObjects())
                {
                    foreach (Camera cam in root.GetComponentsInChildren<Camera>(true)) cam.enabled = false;
                    foreach (EventSystem es in root.GetComponentsInChildren<EventSystem>(true)) es.enabled = false;
                    foreach (AudioListener al in root.GetComponentsInChildren<AudioListener>(true)) al.enabled = false;
                }

                // Run 内画布先补常驻相机（必须在 Build 之前：装配期实例会缓存 canvas.worldCamera）
                foreach (GameObject root in newScene.GetRootGameObjects())
                foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
                    canvas.worldCamera = _camera;

                return FindRunScope(newScene) != null;
            }
            finally
            {
                // 常驻 EventSystem 只在这里恢复：早退与异常都不许让主菜单失去输入
                if (resident) resident.enabled = true;
            }
        }

        // Run 场景里唯一 autoRun 关闭的整局 scope：就地手动 Build，Build 回调负责把段编排者入口注入本类
        private void BuildRunScope()
        {
            LifetimeScope runScope = FindRunScope(_runScene);
            if (runScope == null)
            {
                Debug.LogError($"[{nameof(MainMenuFlow)}] 场景 {_runSceneName} 里没有 LifetimeScope，整局容器不会构建");
                return;
            }

            runScope.Build();
        }

        private static LifetimeScope FindRunScope(Scene scene_)
        {
            foreach (GameObject root in scene_.GetRootGameObjects())
            {
                LifetimeScope found = root.GetComponentInChildren<LifetimeScope>(true);
                if (found != null) return found;
            }

            return null;
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
            await UnloadRunSceneAsync();
            HideOverlays(); // 整局内开过的常驻弹层（设置/放弃）随回菜单一并收起
            if (_mainMenuPanel) _mainMenuPanel.Show();
            _inRun = false;
        }

        // 卸载整局场景：句柄可能已失效（外部卸载），此时 UnloadSceneAsync 返回 null，直接跳过
        private async UniTask UnloadRunSceneAsync()
        {
            try
            {
                if (_runScene.IsValid() && _runScene.isLoaded)
                    await SceneManager.UnloadSceneAsync(_runScene.name).ToUniTask();
            }
            finally
            {
                _runScene = default;
            }
        }

        #endregion
    }
}
