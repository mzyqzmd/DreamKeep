using System.Collections;
using _DreamKeep.GameBattle;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace _DreamKeep.Presentation
{
    /// <summary>通用段场景加载器（住 Scene_MainMenu 常驻层）
    /// 任意段场景（战斗/商店/事件/休息）都通过 SceneManager 以 Additive 加载，加载完成后把段内 LifetimeScope 的
    /// 段场景无相机、复用常驻主相机，加载后补挂到段内每个 Canvas。
    /// 奖励面板"前进"按钮回调临时重载首段（地图系统做好后改为进入地图）。</summary>
    public sealed class SegmentSceneLoader : MonoBehaviour
    {
        [Tooltip("常驻主相机：段内 Screen Space-Camera 画布共用它")] [SerializeField]
        private Camera _camera;

        [Tooltip("常驻组合根：段 scope 的父（同场景，运行时兜底查找）")] [SerializeField]
        private RunLifetimeScope _runScope;

        [Tooltip("首段场景名（临时：主菜单点开始后加载；未来由地图决定）")] [SerializeField]
        private string _firstSegmentName = "Scene_Battle";

        [Tooltip("顶栏节点：点击测试按钮后激活显示（其下 LayerBar/遗物栏/药水栏随之显示）")] [SerializeField]
        private GameObject _topBar;

        private BattleSession _session;
        private Scene _currentSegment;
        private bool _finishing;
        private bool _loading;

        [Inject]
        public void Construct(BattleSession session_) => _session = session_;

        [Button("战斗开始测试入口", EButtonEnableMode.Playmode)]
        // 测试入口（主菜单按钮直连）：激活顶栏后加载并开启第一段。纯按钮触发，进入主菜单不自动开战。
        public void StartFirstSegment()
        {
            if (_loading || _currentSegment.IsValid() && _currentSegment.isLoaded) return;
            if (_topBar) _topBar.SetActive(true);
            _loading = true;
            StartCoroutine(LoadSegmentRoutine(_firstSegmentName));
        }

        private void Start()
        {
            if (_runScope == null) _runScope = FindObjectOfType<RunLifetimeScope>();
        }

        private IEnumerator LoadSegmentRoutine(string name_)
        {
            yield return SceneManager.LoadSceneAsync(name_, LoadSceneMode.Additive);

            Scene newScene = SceneManager.GetSceneByName(name_);
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

            if (segmentScope == null)
            {
                _loading = false;
                yield break;
            }

            // 跨场景父引用无法序列化，运行时赋值后再构建（BattleLifetimeScope 已关 autoRun）
            segmentScope.parentReference.Object = _runScope;
            segmentScope.Build();

            // 段内画布补常驻相机（沿用原 BattleSegmentLauncher.Spawn 的逻辑）
            foreach (GameObject root in newScene.GetRootGameObjects())
            foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
                canvas.worldCamera = _camera;

            // 新段已起，再卸载旧段，避免两段时间同帧共存导致全局注入互相污染
            if (_currentSegment.IsValid() && _currentSegment.isLoaded && _currentSegment.name != name_)
                yield return SceneManager.UnloadSceneAsync(_currentSegment);
            _currentSegment = newScene;
            _loading = false; // 段已就绪：加载守卫到此为止，后续由 _currentSegment 与会话状态表达"段在场"
        }

        private void Update()
        {
            if (_finishing || _session == null || !_session.IsOpen || _session.Loop == null ||
                !_session.Loop.IsFinished)
                return;
            // 先关句柄（常驻药水栏要在旧段的目标中间件还活着时注销瞄准）再卸载段（奖励面板尚未接入，本次不弹）
            _finishing = true;
            _session.Close();
            if (_currentSegment.IsValid() && _currentSegment.isLoaded)
                StartCoroutine(UnloadAndReward(_currentSegment.name));
            else
                _finishing = false;
        }


        private IEnumerator UnloadAndReward(string name_)
        {
            yield return SceneManager.UnloadSceneAsync(name_);
            _currentSegment = default;
            _finishing = false;
            _loading = false;
        }
    }
}