using _DreamKeep.GameBattle;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
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
    /// 奖励面板"前进"按钮回调临时重载首段（地图系统做好后改为进入地图）。</summary>
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

        private BattleSession _session;
        private Scene _currentSegment;
        private bool _finishing;
        private bool _loading;

        private void Update()
        {
            if (_finishing || _session == null || !_session.IsOpen || _session.Loop == null ||
                !_session.Loop.IsFinished)
                return;
            // 先关句柄（常驻药水栏要在旧段的目标中间件还活着时注销瞄准）再卸载段（奖励面板尚未接入，本次不弹）
            _finishing = true;
            _session.Close();
            if (_currentSegment.IsValid() && _currentSegment.isLoaded)
                UnloadSegmentAsync(_currentSegment.name).Forget();
            else
                _finishing = false;
        }

        [Inject]
        public void Construct(BattleSession session_) => _session = session_;

        [Button("战斗开始测试入口", EButtonEnableMode.Playmode)]
        // 测试入口（主菜单按钮直连）：激活顶栏后加载并开启第一段。纯按钮触发，进入主菜单不自动开战。
        public void StartFirstSegment()
        {
            if (_loading || _currentSegment.IsValid() && _currentSegment.isLoaded) return;
            _loading = true;
            LoadSegmentAsync(_firstSegmentName).Forget();
        }

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
