using System;
using _DreamKeep.CommonPresentation;
using _DreamKeep.GameRun;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>整局段编排者（住 Scene_Run）：订阅地图选点，进段意图交 RunFlow 映射，
    /// 战斗收尾经段域 Loop 的完成/退出事实驱动（各段入口同形态），场景装卸交给 SegmentSceneLoader，自己只管转场闸门与段关闭；
    /// 换段/卸段全程包在 UI_FadeScreen 黑屏过渡内（幕布节点同场景拖引用，缺失时退化直跑）。
    /// 回主菜单只上报事实：本类不认识常驻主菜单编排者，终点确认经注入的委托出口上报，
    /// 本侧仅负责收尾清理（归位挂起态、关会话、收结果面板、卸段），卸载 Scene_Run 由常驻方执行。</summary>
    public sealed class SegmentSceneManager : MonoBehaviour
    {
        [Tooltip("常驻组合根：段 scope 的父（同场景，运行时兜底查找）")] [SerializeField]
        private RunLifetimeScope _runScope;

        private Camera _camera; // 常驻主相机（住 Scene_MainMenu）：跨场景引用不可序列化，进局时经 BindCamera 注入

        [Tooltip("整局顶栏：进段场景后显示（同场景，Inspector 拖引用，零查找）")] [SerializeField]
        private UI_TopBar _topBar;

        [Tooltip("地图编排器：节点点击上报的来源（同场景，Inspector 拖引用）")] [SerializeField]
        private UI_MapView _mapView;

        [Tooltip("整局结果面板：面板不自己关，回主菜单清理时在清理链内收起（同场景，Inspector 拖引用）")] [SerializeField]
        private UI_ResultPanel _resultPanel;

        [Tooltip("黑屏过渡幕：换段/卸段时盖住画面（同场景，Inspector 拖引用，缺失退化直跑）")] [SerializeField]
        private UI_FadeScreen _fadeScreen;

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

        private BattleSession _session;
        private BattleStage _stage;
        private RunState _run;
        private PopPanelFactory _popFactory;
        private SegmentSceneLoader _loader;
        private bool _switching; // 段装卸中：卸旧段 + 装新段
        private Action _returnToMainMenu; // 回主菜单请求出口：常驻主菜单编排者经 BindReturnHandler 注入

        [Inject]
        public void Construct(BattleSession session_, BattleStage stage_, RunState run_, PopPanelFactory popFactory_)
        {
            _session = session_;
            _stage = stage_;
            _run = run_;
            _popFactory = popFactory_;
            _loader = new SegmentSceneLoader(_camera, _runScope);
            if (_mapView) _mapView.NodeClicked += OnMapNodeClicked;
        }

        #region 对外：回主菜单通道（常驻方注入出口与本侧清理）

        /// <summary>注入常驻主相机并据此重建段装卸器：常驻相机跨场景不可序列化，进局时由容器回调转交。</summary>
        public void BindCamera(Camera camera_)
        {
            _camera = camera_;
            _loader = new SegmentSceneLoader(_camera, _runScope);
        }

        /// <summary>注入回主菜单请求出口：终点确认只上报事实，本类不认识常驻编排者。</summary>
        public void BindReturnHandler(Action handler_) => _returnToMainMenu = handler_;

        /// <summary>整局终点（通关/战败）确认后的请求上报：常驻方黑屏收尾并卸载本场景。</summary>
        public void RequestReturnToMainMenu() => _returnToMainMenu?.Invoke();

        /// <summary>回主菜单前的本侧清理：归位挂起态、关会话、收结果面板与地图、卸掉残段（不揭幕）；
        /// 返回即清理完毕，此后本场景可被调用方卸载。</summary>
        public async UniTask PrepareReturnToMenuAsync()
        {
            _run.SetSegmentPending(false);
            CloseBattleSession();
            if (_resultPanel) _resultPanel.Hide();
            await _loader.UnloadCurrentSegmentAsync();
            if (_topBar) _topBar.CloseMap();
            if (_topBar) _topBar.gameObject.SetActive(false);
        }

        #endregion

        // 问号节点在 RunState.EnterMapNode 内进入即揭示真实类型，这里按 ResolvedType 分派段种类
        private void OnMapNodeClicked(MapNode node_)
        {
            if (_switching) return; // 换段期间忽略二次点击：不能让域推进了而段没切
            _run.EnterMapNode(node_);
            StartSegment(RunFlow.SegmentKindOf(node_.ResolvedType));
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

        /// <summary>按段种类开启一段（"选定下一个节点"）：收起地图与奖励面板、关旧会话、卸掉旧段后再加载新段。</summary>
        public void StartSegment(ESegmentKind kind_)
        {
            if (_switching) return;
            _switching = true;
            _run.SetSegmentPending(true); // 进段即挂起整图可走态：内容没完成前一个节点都点不了、也不会有呼吸
            SwitchSegmentAsync(SegmentNameOf(kind_)).Forget();
        }

        #region 段收尾：继续只关会话与开地图，卸载推迟到加载下一个段

        /// <summary>段完成（各段入口订阅段私有完成事实后调用）：关掉战斗会话（若开着）、打开地图，
        /// 段场景留在场上，直到 <see cref="StartSegment"/> 装载下一个段时才卸载。</summary>
        public void CompleteSegment()
        {
            if (_switching) return;
            _run.SetSegmentPending(false); // 内容已通过时本就解除了，这里兜住异常收尾
            CloseBattleSession();
            if (_topBar) _topBar.OpenMap();
        }

        #endregion

        /// <summary>战斗退出（战败 / 无奖励 / 掷空 / 弹板失败经段收尾事实触发）：不开地图，关会话后经黑屏过渡卸载段。</summary>
        public void FinishSegment()
        {
            if (_switching) return;
            _switching = true;
            _run.SetSegmentPending(false);
            CloseBattleSession();
            EndSegmentAsync().Forget();
        }

        // 会话与表现句柄成对关：先 session（常驻药水栏借旧表现引用注销瞄准，此时旧中间件还活着）后 stage
        private void CloseBattleSession()
        {
            if (!_session.IsOpen) return;
            _session.Close();
            _stage.Close();
        }

        // 过渡分发：幕布拖引用缺失时退化为直跑（不黑屏不挡功能）
        private UniTask CoverRunAsync(Func<UniTask> work_) => _fadeScreen ? _fadeScreen.CoverAsync(work_) : work_();

        private async UniTask EndSegmentAsync()
        {
            try
            {
                await CoverRunAsync(() => _loader.UnloadCurrentSegmentAsync());
            }
            finally
            {
                _switching = false; // 揭幕后再放行：黑屏淡出期间不接新入口
            }
        }

        private async UniTask SwitchSegmentAsync(string name_)
        {
            try
            {
                await CoverRunAsync(() => SwitchSegmentCoreAsync(name_));
            }
            finally
            {
                _switching = false; // 揭幕后再放行：黑屏淡出期间不接新入口
            }
        }

        // 黑屏内的换段链：先卸旧段再装新段的顺序与加载失败的「本局作废」日志口径不变
        private async UniTask SwitchSegmentCoreAsync(string name_)
        {
            try
            {
                // 进新段前收起上一场的奖励面板与地图：面板不随「继续」关闭，留着会挡住下一场
                // （只关已建出的实例：为关一次而懒建，会在常驻画布凭空多出面板克隆体）
                if (_popFactory) _popFactory.TryCloseRewardPanel();
                if (_topBar) _topBar.CloseMap();
                CloseBattleSession();
                // 必须先卸旧段：新段构建时会按类型扫场景注册，两段同类组件共存会互相污染
                await _loader.UnloadCurrentSegmentAsync();
                if (!await _loader.LoadSegmentAsync(name_))
                    Debug.LogError($"[{nameof(SegmentSceneManager)}] 段场景加载失败（不在构建列表或句柄失效），本局作废");
                else if (_topBar) _topBar.gameObject.SetActive(true); // 进入段场景后整局顶栏才显示
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError($"[{nameof(SegmentSceneManager)}] 段装配异常，本局作废：{e.Message}");
            }
        }
    }
}
