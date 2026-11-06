using System;
using System.Collections.Generic;
using _DreamKeep.CommonPresentation;
using _DreamKeep.GameBattle;
using _DreamKeep.GameRun;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>整局段编排者（住 Scene_MainMenu 常驻层）：订阅地图选点与战斗会话开关，
    /// 进段意图交 RunFlow 映射、战斗终局就地裁决，场景装卸交给 SegmentSceneLoader，自己只管转场闸门与奖励面板。
    /// 段收尾与卸载分离：休息、先古与战斗（有奖励）点「继续」只关会话并打开地图，段场景原样留在屏幕上，
    /// 到加载下一个段（选定节点）时才先卸旧段再装新段；战败与无奖励胜利仍立即收段。</summary>
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
        private RunState _run;
        private PopPanelFactory _popFactory;
        private SegmentSceneLoader _loader;
        private bool _switching; // 段装卸中：卸旧段 + 装新段

        [Inject]
        public void Construct(BattleSession session_, RunState run_, PopPanelFactory popFactory_)
        {
            _session = session_;
            _run = run_;
            _popFactory = popFactory_;
            _loader = new SegmentSceneLoader(_camera, _runScope);
            if (_mapView) _mapView.NodeClicked += OnMapNodeClicked;
            _session.Changed += OnBattleSessionChanged;
        }

        // 会话每次 Open 起一条一次性等待；Close 时 Loop 已清空，早退不重复起任务
        private void OnBattleSessionChanged()
        {
            if (_session.Loop == null) return;
            AwaitBattleEndAsync(_session.Loop).Forget();
        }

        private async UniTask AwaitBattleEndAsync(BattleLoop loop_)
        {
            await loop_.WaitFinishedAsync();
            if (_session.Loop != loop_) return; // 段已被切走或会话已关，旧战斗的完成信号不作数

            // 胜利即通过：地图推进不等奖励面板的「继续」
            if (loop_.IsVictory) _run.CompleteMapNode();

            // 胜利且有奖励配置时弹面板；弹板失败、战败、Boss 等无奖励胜利都降级为立即收段
            if (loop_.IsVictory && _session.Reward != null && TryShowReward()) return;
            FinishSegment();
        }

        // 就地结算：段与会话都还活着，药水/遗物栏在领取瞬间还能正常刷新；掷骰或弹板失败均降级为直接收段
        private bool TryShowReward()
        {
            List<RunReward> rewards = null;
            try
            {
                rewards = EncounterRewardRoller.Roll(_session.Reward, _run, message_ => Debug.LogWarning(message_));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            if (rewards == null || rewards.Count == 0) return false;
            try
            {
                _popFactory.AcquireRewardPanel().Show("战斗奖励", rewards, CompleteSegment);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
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

        /// <summary>段完成（各段入口与战斗奖励面板的「继续」共用）：关掉战斗会话（若开着）、打开地图，
        /// 段场景留在场上，直到 <see cref="StartSegment"/> 装载下一个段时才卸载。</summary>
        public void CompleteSegment()
        {
            if (_switching) return;
            _run.SetSegmentPending(false); // 内容已通过时本就解除了，这里兜住异常收尾
            if (_session.IsOpen) _session.Close(); // 先关句柄：常驻药水栏要在旧段中间件还活着时注销瞄准
            if (_topBar) _topBar.OpenMap();
        }

        #endregion

        // 战败或无奖励胜利（Boss）：不开地图，关会话后立即卸载段
        private void FinishSegment()
        {
            if (_switching) return;
            _switching = true;
            _run.SetSegmentPending(false);
            if (_session.IsOpen) _session.Close();
            EndSegmentAsync().Forget();
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
                if (_session.IsOpen) _session.Close();
                // 必须先卸旧段：新段构建时会按类型扫场景注册，两段同类组件共存会互相污染
                await _loader.UnloadCurrentSegmentAsync();
                if (!await _loader.LoadSegmentAsync(name_)) _run.SetSegmentPending(false); // 进段失败：解除挂起
                else if (_topBar) _topBar.gameObject.SetActive(true); // 进入段场景后整局顶栏才显示
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                _run.SetSegmentPending(false); // 装配异常同样兜底：段没跑起来，地图必须保持可选
            }
            finally
            {
                _switching = false;
            }
        }
    }
}
