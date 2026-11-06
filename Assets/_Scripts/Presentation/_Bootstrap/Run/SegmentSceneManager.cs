using System;
using _DreamKeep.CommonPresentation;
using _DreamKeep.GameRun;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>整局段编排者（住 Scene_MainMenu 常驻层）：订阅地图选点，进段意图交 RunFlow 映射，
    /// 战斗收尾经段域 Loop 的完成/退出事实驱动（各段入口同形态），场景装卸交给 SegmentSceneLoader，自己只管转场闸门与段关闭。
    /// 段收尾与卸载分离：休息、先古与战斗（有奖励）点「继续」只关会话并打开地图，段场景原样留在屏幕上，
    /// 到加载下一个段（选定节点）时才先卸旧段再装新段；整局终点（通关/战败/将来放弃）经 ReturnToMenu 确认后卸段回主菜单。</summary>
    public sealed class SegmentSceneManager : MonoBehaviour
    {
        [Tooltip("常驻主相机：段内 Screen Space-Camera 画布共用它")] [SerializeField]
        private Camera _camera;

        [Tooltip("常驻组合根：段 scope 的父（同场景，运行时兜底查找）")] [SerializeField]
        private RunLifetimeScope _runScope;

        [Tooltip("整局顶栏：主菜单期间隐藏、进入段场景后显示（同场景，Inspector 拖引用，零查找）")] [SerializeField]
        private UI_TopBar _topBar;

        [Tooltip("地图编排器：节点点击上报的来源（同场景，Inspector 拖引用）")] [SerializeField]
        private UI_MapView _mapView;

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

        /// <summary>战斗退出（战败 / 无奖励 / 掷空 / 弹板失败经段收尾事实触发）：不开地图，关会话后立即卸载段。</summary>
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

        private async UniTask EndSegmentAsync()
        {
            try
            {
                await _loader.UnloadCurrentSegmentAsync();
            }
            finally
            {
                _switching = false;
            }
        }

        private async UniTask SwitchSegmentAsync(string name_)
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
            finally
            {
                _switching = false;
            }
        }

        /// <summary>整局终点确认后回主菜单（通关 / 战败 / 将来主菜单「放弃」三种终点共用）：关会话、卸残段、
        /// 归位挂起态与顶栏；重开一把的入口由主菜单提供。状态不回滚——终点即整局作废，重开由新的整局状态承接。</summary>
        public void ReturnToMenu()
        {
            if (_switching) return; // 收尾中：面板按钮连点不重复触发卸段（与其余三个入口同形态）
            ReturnToMenuAsync().Forget();
        }

        private async UniTask ReturnToMenuAsync()
        {
            _switching = true;
            try
            {
                _run.SetSegmentPending(false);
                CloseBattleSession();
                await _loader.UnloadCurrentSegmentAsync();
                if (_topBar) _topBar.CloseMap();
                if (_topBar) _topBar.gameObject.SetActive(false);
            }
            finally
            {
                _switching = false;
            }
        }
    }
}