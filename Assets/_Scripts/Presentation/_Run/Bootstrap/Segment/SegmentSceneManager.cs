using System;
using _DreamKeep.CommonPresentation;
using _DreamKeep.ConfigData;
using _DreamKeep.EngineUtility;
using _DreamKeep.GameRun;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Presentation
{
    /// <summary>整局段编排者（住 Scene_Run）：常驻方与段流程之间的薄协调外壳。
    /// 换段次序、BGM/顶栏/面板表现、战斗会话开关分别交给三个协作类，本类只做事件接线与流程分派；
    /// 全部 Inspector 引用留在本外壳，不动场景序列化。换段/卸段包在常驻黑屏内，幕布唯一实例住 Scene_MainMenu，
    /// 进局时经 BindFadeScreen 注入，缺失时退化直跑。回主菜单只上报事实，卸载 Scene_Run 由常驻方执行。</summary>
    public sealed class SegmentSceneManager : MonoBehaviour
    {
        [Tooltip("常驻组合根：段 scope 的父（同场景，运行时兜底查找）")] [SerializeField]
        private RunLifetimeScope _runScope;

        [Tooltip("整局顶栏：进段场景后显示（同场景，Inspector 拖引用，零查找）")] [SerializeField]
        private UI_TopBar _topBar;

        [Tooltip("地图编排器：节点点击上报的来源（同场景，Inspector 拖引用）")] [SerializeField]
        private UI_MapView _mapView;

        [Tooltip("整局结果面板：面板不自己关，回主菜单清理时在清理链内收起（同场景，Inspector 拖引用）")] [SerializeField]
        private UI_ResultPanel _resultPanel;

        private UI_FadeScreen _fadeScreen; // 黑屏过渡幕（住 Scene_MainMenu）：跨场景引用不可序列化，进局时经 BindFadeScreen 注入

        [Tooltip("暂停面板：顶栏菜单按钮开合，五条出口在此接线（同场景，Inspector 拖引用）")] [SerializeField]
        private UI_PausePanel _pausePanel;

        /// <summary>战斗段场景名：按段种类映射到构建列表中的场景名。</summary>
        public const string BattleSegmentName = "Scene_Battle";

        /// <summary>先古之民段场景名：按段种类映射到构建列表中的场景名。</summary>
        public const string AncientSegmentName = "Scene_Ancient";

        /// <summary>休息段场景名：按段种类映射到构建列表中的场景名。</summary>
        public const string RestSegmentName = "Scene_Rest";

        /// <summary>宝箱段场景名：按段种类映射到构建列表中的场景名。</summary>
        public const string ChestSegmentName = "Scene_Chest";

        /// <summary>事件段场景名：按段种类映射到构建列表中的场景名。</summary>
        public const string EventSegmentName = "Scene_Event";

        /// <summary>商店段场景名：按段种类映射到构建列表中的场景名。</summary>
        public const string ShopSegmentName = "Scene_Shop";

        private RunProgressService _progress;
        private RunCommandDispatcher _commands;
        private SegmentSceneLoader _loader;
        private SegmentTransitionCoordinator _transition;
        private SegmentPresentationCoordinator _presentation;
        private BattleSessionLifetime _battleSession;
        private Action _returnToMainMenu; // 回主菜单请求出口：常驻主菜单编排者经 BindReturnHandler 注入

        // 整局计时泵：本类随 Scene_Run 生灭，在场即计时；真实时间口径不受 timeScale 影响
        private void Update() => _commands?.Tick(Time.unscaledDeltaTime);

        [Inject]
        public void Construct(BattleSession session_, BattleStage stage_, RunState run_, RunProgressService progress_,
            PopPanelFactory popFactory_, AudioRoot audioRoot_, RunCommandDispatcher commands_)
        {
            _progress = progress_;
            _commands = commands_;
            _loader = new SegmentSceneLoader(_runScope); // 相机由 BindCamera 运行时注入，构造期不持有
            _battleSession = new BattleSessionLifetime(session_, stage_);
            _presentation = new SegmentPresentationCoordinator(_topBar, _resultPanel, _pausePanel, audioRoot_, run_); // App 层面板入口在 BindAppPanels 时补装
            _transition = new SegmentTransitionCoordinator(_loader, () => _fadeScreen, () => popFactory_,
                () => _presentation.CloseMap(), () => _presentation.ToggleTopBar(true),
                _battleSession.Close, nameof(SegmentSceneManager));

            if (_mapView) _mapView.NodeClicked += OnMapNodeClicked;
            // 暂停面板常常预摆失活：接线挂在本类（随整局容器装配必跑），不依赖面板自身 Awake
            _presentation.WirePausePanel(ClosePausePanel, OnSettingClicked, OnEncyclopediaClicked,
                OnGiveUpClicked, OnSaveAndQuitClicked);
        }

        #region 对外：常驻方运行时注入（相机/幕布/回主菜单出口）与回主菜单清理

        /// <summary>注入常驻主相机并转交段装卸器：常驻相机跨场景不可序列化，进局时由容器回调转交。</summary>
        public void BindCamera(Camera camera_) => _loader.BindCamera(camera_);

        /// <summary>注入常驻黑屏过渡幕：幕布住 Scene_MainMenu、本类随 Scene_Run 生灭，只能运行时转交。</summary>
        public void BindFadeScreen(UI_FadeScreen fadeScreen_) => _fadeScreen = fadeScreen_;

        /// <summary>注入回主菜单请求出口：终点确认只上报事实，本类不认识常驻编排者。</summary>
        public void BindReturnHandler(Action handler_) => _returnToMainMenu = handler_;

        /// <summary>注入 App 层常驻面板入口（设置开合、百科开合、带确认语义的放弃确认）：面板住 Scene_MainMenu，只能运行时转交。</summary>
        public void BindAppPanels(Action showSetting_, Action showEncyclopedia_, Action<Action> showGiveUp_)
        {
            _presentation.BindAppPanels(showSetting_, showEncyclopedia_, showGiveUp_);
            _presentation.WirePausePanel(ClosePausePanel, OnSettingClicked, OnEncyclopediaClicked,
                OnGiveUpClicked, OnSaveAndQuitClicked);
        }

        /// <summary>整局终点（通关/战败）确认后的请求上报：本局已结束，删档后由常驻方黑屏收尾并卸载本场景。</summary>
        public void RequestReturnToMainMenu()
        {
            _progress.DeleteSave();
            _returnToMainMenu?.Invoke();
        }

        #endregion

        /// <summary>回主菜单前的本侧清理：归位挂起态、关会话、收结果面板/地图/暂停面板、卸掉残段（不揭幕）；
        /// 返回即清理完毕，此后本场景可被调用方卸载。</summary>
        public async UniTask PrepareReturnToMenuAsync()
        {
            _progress.LeaveSegment();
            _battleSession.Close();
            ClosePausePanel();
            _presentation.HideResultPanel();
            await _loader.UnloadCurrentSegmentAsync();
            _presentation.CloseMap();
            _presentation.ToggleTopBar(false);
        }

        #region 暂停面板五条出口：继续 / 设置 / 百科 / 放弃 / 保存并退出

        private void ClosePausePanel() => _presentation.HidePausePanel();

        // 设置面板住常驻主菜单画布（层级在整局之上），本类只转发开合
        private void OnSettingClicked() => _presentation.ShowSetting();

        // 百科面板住常驻主菜单画布（与设置同一份），本类只转发开合
        private void OnEncyclopediaClicked() => _presentation.ShowEncyclopedia();

        // 放弃先过二次确认（面板同样住常驻画布），确认后才真的结束本局
        private void OnGiveUpClicked() => _presentation.ShowGiveUp(ConfirmGiveUp);

        // 确认放弃：删档 → 收面板与地图 → 关战斗会话（战斗停下，免得继续推进覆盖结算文案）→ 弹战败结算
        private void ConfirmGiveUp()
        {
            _progress.DeleteSave();
            _progress.LeaveSegment();
            ClosePausePanel();
            _presentation.CloseMap();
            _battleSession.Close();
            _presentation.ShowDefeatResult();
        }

        // 保存并退出：段内一律不写盘——磁盘停留在最近提交点（地图选点或战斗胜利检查点），
        // 读档即回到该点；主菜单据磁盘真值亮出「继续」
        private void OnSaveAndQuitClicked() => _returnToMainMenu?.Invoke();

        #endregion

        // 问号节点在 RunState.EnterMapNode 内进入即揭示真实类型，这里按 ResolvedType 分派段种类
        private void OnMapNodeClicked(MapNode node_)
        {
            if (_transition.Switching) return; // 换段期间忽略二次点击：不能让域推进了而段没切
            _presentation.PlayUiClick();
            _commands.EndRewardClaim(); // 选点即领奖终点：先清待领清单，再让 EnterNode 落选点检查点
            StartSegmentAsync(_progress.EnterNode(node_)).Forget();
        }

        private static string SegmentNameOf(ESegmentKind kind_) => kind_ switch
        {
            ESegmentKind.Battle => BattleSegmentName,
            ESegmentKind.Shop => ShopSegmentName,
            ESegmentKind.Rest => RestSegmentName,
            ESegmentKind.Treasure => ChestSegmentName,
            ESegmentKind.Event => EventSegmentName,
            ESegmentKind.Ancient => AncientSegmentName,
            _ => throw new ArgumentOutOfRangeException(nameof(kind_), kind_, "段种类没有对应段场景")
        };

        /// <summary>按段种类开启一段（"选定下一个节点"）：收起地图与奖励面板、关旧会话、卸掉旧段后再加载新段。
        /// 返回的任务在段装配完成（含黑幕揭开）后结束：常驻方据此把黑幕一直盖到首段就绪。</summary>
        public async UniTask StartSegmentAsync(ESegmentKind kind_)
        {
            if (_transition.Switching) return;
            _transition.Switching = true;
            _progress.EnterSegment();
            _presentation.SwitchBgmOf(kind_);
            try
            {
                await _transition.CoverAsync(() => _transition.SwitchSegmentAsync(SegmentNameOf(kind_)));
            }
            finally
            {
                _transition.Switching = false; // 揭幕后再放行：黑屏淡出期间不接新入口
            }
        }

        #region 段收尾：继续只关会话与开地图，卸载推迟到加载下一个段

        /// <summary>段完成（各段入口订阅段私有完成事实后调用）：关掉战斗会话（若开着）、打开地图，
        /// 段场景留在场上，直到 <see cref="StartSegmentAsync"/> 装载下一个段时才卸载。</summary>
        public void CompleteSegment()
        {
            if (_transition.Switching) return;
            _progress.LeaveSegment(); // 内容已通过时本就解除了，这里兜住异常收尾
            _progress.MarkOnMap();
            // 段内不写盘：段收益要到「在地图上选中下一个节点」才随提交点固化
            _battleSession.Close();
            _presentation.PlayBgmRun();
            _presentation.OpenMap();
        }

        /// <summary>重复点奖励「继续」时只重开地图：段完成已由首次点击走完，不重跑结算。</summary>
        public void OpenMap() => _presentation.OpenMap();

        #endregion

        /// <summary>战斗退出（战败 / 无奖励 / 掷空 / 弹板失败经段收尾事实触发）：不开地图，关会话后经黑屏过渡卸载段。</summary>
        public void FinishSegment()
        {
            if (_transition.Switching) return;
            _transition.Switching = true;
            _progress.LeaveSegment();
            _progress.ClearPendingRewards(); // 无奖励退出：清单不会再用，清掉免得下次读档误判
            _battleSession.Close();
            UnloadCoveredAsync().Forget();

            // 黑幕内卸段：揭幕后才放行下一次进段
            async UniTask UnloadCoveredAsync()
            {
                try
                {
                    await _transition.CoverAsync(() => _loader.UnloadCurrentSegmentAsync());
                }
                finally
                {
                    _transition.Switching = false;
                }
            }
        }
    }
}
