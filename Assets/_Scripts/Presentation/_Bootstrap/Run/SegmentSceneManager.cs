using System;
using System.Collections.Generic;
using _DreamKeep.CommonPresentation;
using _DreamKeep.GameRun;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace _DreamKeep.Presentation
{
    /// <summary>通用段场景加载管理器（住 Scene_MainMenu 常驻层）
    /// 任意段场景（战斗/商店/事件/休息）都通过 SceneManager 以 Additive 加载，加载到待激活态时先把段内自带的
    /// 相机 / EventSystem / AudioListener 禁用（仍保留在场景资产供查看），再激活叠加，复用常驻主相机，
    /// 加载后把常驻主相机补挂到段内每个 Canvas。
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

        [Tooltip("首段场景名（临时：主菜单点开始后加载；未来由地图决定）")] [SerializeField]
        private string _firstSegmentName = "Scene_Battle";

        /// <summary>先古之民段场景名：段内各处与开发面板共用，避免散落字面量。</summary>
        public const string AncientSegmentName = "Scene_Ancient";

        /// <summary>休息段场景名：段内各处与开发面板共用，避免散落字面量。</summary>
        public const string RestSegmentName = "Scene_Rest";

        private BattleSession _session;
        private RunState _run;
        private PopFactory _popFactory;
        private Scene _currentSegment;
        private bool _awaitingReward; // 奖励面板已弹，等玩家点继续
        private bool _switching; // 段装卸中：卸旧段 + 装新段

        private void Update()
        {
            // 等玩家领奖、或正在换段时不再重复触发；点继续后会话已关，这里也自然早退
            if (_awaitingReward || _switching) return;
            if (_session == null || !_session.IsOpen || !_session.Loop.IsFinished) return;

            // 胜利且本场配了奖励：段与会话都还活着时就地结算——药水/遗物栏在领取瞬间还能正常刷新；
            // 点继续只关会话、开地图（见 CompleteSegment），段留在场上等下一个节点
            if (_session.Loop.IsVictory && _session.Reward != null)
            {
                List<RunReward> rewards = null;
                try
                {
                    rewards = EncounterRewardRoller.Roll(_session.Reward, _run);
                }
                catch (Exception e)
                {
                    Debug.LogException(e); // 掷骰坏了也要能离开战斗，降级为直接收段
                }

                if (rewards != null && rewards.Count > 0)
                {
                    try
                    {
                        _awaitingReward = true;
                        _popFactory.AcquireRewardPanel().Show("战斗奖励", rewards, CompleteSegment);
                        return;
                    }
                    catch (Exception e)
                    {
                        _awaitingReward = false;
                        Debug.LogException(e);
                    }
                }
            }

            FinishSegment(); // 战败、Boss 等无奖励遭遇或结算失败：立即收段
        }

        [Inject]
        public void Construct(BattleSession session_, RunState run_, PopFactory popFactory_)
        {
            _session = session_;
            _run = run_;
            _popFactory = popFactory_;
        }

        /// <summary>激活顶栏后加载并开启第一段（Play 模式测试入口，由 Tools/DreamKeep/开发面板 调用）。</summary>
        public void StartFirstSegment() => StartSegment(_firstSegmentName);

        /// <summary>按段名加载并开启一段（"选定下一个节点"）：收起地图与奖励面板、关旧会话、卸掉旧段后再加载新段。</summary>
        public void StartSegment(string name_)
        {
            if (_switching) return;
            _switching = true;
            _awaitingReward = false;
            SwitchSegmentAsync(name_).Forget();
        }

        #region 段收尾：继续只关会话与开地图，卸载推迟到加载下一个段

        /// <summary>段完成（休息/先古入口与战斗奖励面板的「继续」共用）：关掉战斗会话（若开着）、打开地图，
        /// 段场景留在场上，直到 <see cref="StartSegment"/> 装载下一个段时才卸载。</summary>
        public void CompleteSegment()
        {
            if (_switching) return;
            _awaitingReward = false;
            if (_session.IsOpen) _session.Close(); // 先关句柄：常驻药水栏要在旧段中间件还活着时注销瞄准
            if (_topBar) _topBar.OpenMap();
        }

        #endregion

        // 战败或无奖励胜利（Boss）：不开地图，关会话后立即卸载段；战败/胜利面板落地前保持这一收尾
        private void FinishSegment()
        {
            if (_switching) return;
            _switching = true;
            _awaitingReward = false;
            if (_session.IsOpen) _session.Close();
            EndSegmentAsync().Forget();
        }

        private async UniTask EndSegmentAsync()
        {
            try
            {
                await UnloadCurrentSegmentAsync();
            }
            finally
            {
                _switching = false;
            }
        }

        #region 段场景加载与卸载

        private async UniTask SwitchSegmentAsync(string name_)
        {
            try
            {
                // 进新段前收起上一场的奖励面板与地图：面板不随「继续」关闭，留着会挡住下一场
                if (_popFactory) _popFactory.AcquireRewardPanel().Close();
                if (_topBar) _topBar.CloseMap();
                if (_session.IsOpen) _session.Close();
                // 必须先卸旧段：新段构建时会按类型扫场景注册，两段同类组件共存会互相污染
                await UnloadCurrentSegmentAsync();
                await LoadSegmentAsync(name_);
            }
            finally
            {
                _switching = false;
            }
        }

        private async UniTask LoadSegmentAsync(string name_)
        {
            // 加载到待激活态，先不激活段场景，避免相机闪帧。
            AsyncOperation load = SceneManager.LoadSceneAsync(name_, LoadSceneMode.Additive);
            if (load == null) return;

            load.allowSceneActivation = false;
            await UniTask.WaitUntil(() => load.progress >= 0.9f);

            // allowSceneActivation=false 期间场景根不可访问，无法在激活前关段内 EventSystem。
            // 改为激活前临时关常驻 EventSystem（此刻全局列表为空），激活后再关段内的、最后恢复常驻：
            // 全程至多一个 active EventSystem，从根上避免 "There can be only one active Event System"。
            EventSystem resident = EventSystem.current;
            if (resident) resident.enabled = false;

            try
            {
                load.allowSceneActivation = true;
                await load.ToUniTask();

                // 段已激活、根可访问：先记账，后面任何早退都要留得下可回收的段
                Scene newScene = SceneManager.GetSceneByName(name_);
                if (!newScene.IsValid()) return;
                _currentSegment = newScene;

                // 关掉段内自带相机 / EventSystem / AudioListener（组件级，资产仍保留供编辑查看）
                foreach (GameObject root in newScene.GetRootGameObjects())
                {
                    foreach (Camera cam in root.GetComponentsInChildren<Camera>(true)) cam.enabled = false;
                    foreach (EventSystem es in root.GetComponentsInChildren<EventSystem>(true)) es.enabled = false;
                    foreach (AudioListener al in root.GetComponentsInChildren<AudioListener>(true)) al.enabled = false;
                }

                LifetimeScope segmentScope = null;
                foreach (GameObject root in newScene.GetRootGameObjects())
                {
                    LifetimeScope found = root.GetComponentInChildren<LifetimeScope>(true);
                    if (found != null && found != _runScope)
                    {
                        segmentScope = found;
                        break;
                    }
                }

                if (segmentScope == null) return;

                // 段内画布先补常驻相机（必须在 Build 之前：注入与卡池实例生成都在 Build 内、且会缓存 canvas.worldCamera，
                // 挪前才能保证 BattleTargetDirector / UI_HandView / UI_HoverOffset 读到常驻相机，根治卡不跟手与箭头冻结）
                foreach (GameObject root in newScene.GetRootGameObjects())
                foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
                    canvas.worldCamera = _camera;

                // 跨场景父引用无法序列化，运行时赋值后再构建（BattleLifetimeScope 已关 autoRun）
                segmentScope.parentReference.Object = _runScope;
                segmentScope.Build();

                // 进入段场景后，整局顶栏才显示
                if (_topBar) _topBar.gameObject.SetActive(true);
            }
            finally
            {
                // 常驻 EventSystem 只在这里恢复：早退与异常都不许让整局 UI 失去输入
                if (resident) resident.enabled = true;
            }
        }

        // 调用方负责 _switching：换段流程要等新段装载完才落闸
        private async UniTask UnloadCurrentSegmentAsync()
        {
            try
            {
                // 场景可能已被别处卸载：此时 UnloadSceneAsync 返回 null，直接跳过
                if (_currentSegment.IsValid() && _currentSegment.isLoaded)
                    await SceneManager.UnloadSceneAsync(_currentSegment.name).ToUniTask();
            }
            finally
            {
                _currentSegment = default;
            }
        }

        #endregion
    }
}
