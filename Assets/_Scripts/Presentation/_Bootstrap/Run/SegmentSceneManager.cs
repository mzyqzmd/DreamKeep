using System;
using System.Collections.Generic;
using _DreamKeep.CommonPresentation;
using _DreamKeep.GameBattle;
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
    /// 战斗胜利且本场配了奖励时，段与会话仍活着就先弹奖励面板；点"继续"打开地图占位、关会话并卸段，
    /// 奖励面板本身不随之关闭（未领的条目仍留在上面），只在进入下一个段前收起。战败或没配奖励则照旧立即收段。</summary>
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

        private BattleSession _session;
        private RunState _run;
        private PopFactory _popFactory;
        private Scene _currentSegment;
        private bool _awaitingReward;
        private bool _finishing;
        private bool _loading;

        private void Update()
        {
            // 等玩家领奖期间不再重复触发；_finishing 只在真正收段时置位，避免掷骰出异常把流程锁死
            if (_awaitingReward || _finishing) return;
            if (_session == null || !_session.IsOpen || !_session.Loop.IsFinished) return;

            // 胜利且本场配了奖励：段与会话都还活着时就地结算——药水/遗物栏在领取瞬间还能正常刷新；
            // 点继续才收段开地图（见 OnRewardProceeded）
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
                        _popFactory.AcquireRewardPanel().Show("战斗奖励", rewards, OnRewardProceeded);
                        // 面板是全屏模态：把顶栏提上来，玩家才能在看奖励时喝药水/看金币
                        if (_topBar) _topBar.BringToFront();
                        return;
                    }
                    catch (Exception e)
                    {
                        _awaitingReward = false;
                        Debug.LogException(e);
                    }
                }
            }

            FinishSegment(); // 战败、Boss、奖励清单为空或结算失败：立即收段
        }

        [Inject]
        public void Construct(BattleSession session_, RunState run_, PopFactory popFactory_)
        {
            _session = session_;
            _run = run_;
            _popFactory = popFactory_;
        }

        // 继续＝玩家要往下走了：开地图占位、收段。奖励面板**不关**（未领的条目还留在上面，领完自然就是空的），
        // 它只在进入下一个段时才收起，免得挡住下一场战斗
        private void OnRewardProceeded()
        {
            if (_topBar) _topBar.OpenMap();
            FinishSegment();
        }

        // 先关句柄（常驻药水栏要在旧段的目标中间件还活着时注销瞄准）再卸载段
        private void FinishSegment()
        {
            if (_finishing) return;
            _finishing = true;
            _awaitingReward = false;
            if (_session.IsOpen) _session.Close();
            if (_currentSegment.IsValid() && _currentSegment.isLoaded)
                UnloadSegmentAsync(_currentSegment.name).Forget();
            else
                _finishing = false;
        }

        /// <summary>激活顶栏后加载并开启第一段（Play 模式测试入口，由 Tools/DreamKeep/开发面板 调用）。</summary>
        public void StartFirstSegment() => StartSegment(_firstSegmentName);

        /// <summary>按段名加载并开启一段（Play 模式测试入口：战斗 / 先古之民等段共用，由开发面板调用）。</summary>
        public void StartSegment(string name_)
        {
            if (_loading || _currentSegment.IsValid() && _currentSegment.isLoaded) return;
            _loading = true;
            // 进新段前收起上一场的奖励面板：它按设计不随「继续」关闭，留着会挡住下一场战斗
            if (_popFactory) _popFactory.AcquireRewardPanel().Close();
            LoadSegmentAsync(name_).Forget();
        }

        #region 非战斗段收尾：先古之民等段领完产出后由段入口调用，战斗会话不经过这里

        /// <summary>收掉当前段：开地图占位并卸段。战斗段的收尾（含会话关闭与奖励结算）仍走 Update 轮询路径。</summary>
        public void CompleteSegment()
        {
            if (_finishing || _loading) return;
            if (_topBar) _topBar.OpenMap();
            if (_currentSegment.IsValid() && _currentSegment.isLoaded)
                UnloadSegmentAsync(_currentSegment.name).Forget();
        }

        #endregion

        #region 段场景加载与卸载

        private async UniTask LoadSegmentAsync(string name_)
        {
            // 加载到待激活态，先不激活段场景，避免相机闪帧。
            AsyncOperation load = SceneManager.LoadSceneAsync(name_, LoadSceneMode.Additive);
            if (load == null)
            {
                _loading = false;
                return;
            }

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

                // 段已激活、根可访问：关掉段内自带相机 / EventSystem / AudioListener（组件级，资产仍保留供编辑查看）
                Scene newScene = SceneManager.GetSceneByName(name_);
                if (!newScene.IsValid()) return;
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

                // 新段已起，再卸载旧段，避免两段时间同帧共存导致全局注入互相污染
                if (_currentSegment.IsValid() && _currentSegment.isLoaded && _currentSegment.name != name_)
                    await SceneManager.UnloadSceneAsync(_currentSegment).ToUniTask();
                _currentSegment = newScene;
            }
            finally
            {
                // 常驻 EventSystem 只在这里恢复：早退与异常都不许让整局 UI 失去输入
                if (resident) resident.enabled = true;
                _loading = false; // 段已就绪或加载失败：加载守卫到此为止
            }
        }

        private async UniTask UnloadSegmentAsync(string name_)
        {
            try
            {
                // 场景可能已被别处卸载：此时 UnloadSceneAsync 返回 null，直接跳过
                if (SceneManager.GetSceneByName(name_).isLoaded)
                    await SceneManager.UnloadSceneAsync(name_).ToUniTask();
            }
            finally
            {
                _currentSegment = default;
                _finishing = false;
                _loading = false;
            }
        }

        #endregion
    }
}
