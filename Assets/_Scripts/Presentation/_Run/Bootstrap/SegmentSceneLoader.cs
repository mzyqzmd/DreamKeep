using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace _DreamKeep.Presentation
{
    /// <summary>段场景装卸器：Additive 加载段场景并装配段 scope，卸载时回收场景句柄。
    /// 加载到待激活态后激活，关段内相机 / AudioListener、给段内 Canvas 补常驻主相机（必须在 Build 之前）。
    /// 常驻相机跨场景不可序列化，由编排者运行时注入，因此本类自带 BindCamera 而不是构造期固定。
    /// 只负责装载，不决定何时进段、何时收段。
    /// 与 MainMenuFlow.LoadRunSceneAsync 是同构片段（待激活态加载 → 激活 → 取句柄 → 关组件 → 补画布相机），改一处必看另一处。</summary>
    public sealed class SegmentSceneLoader
    {
        private readonly RunLifetimeScope _runScope;
        private Scene _currentSegment;
        private Camera _camera;

        public SegmentSceneLoader(RunLifetimeScope runScope_) => _runScope = runScope_;

        /// <summary>注入常驻主相机：段内画布与段 scope 构建都要用它，未注入时按装配错误报一条。</summary>
        public void BindCamera(Camera camera_) => _camera = camera_;

        #region 装载段场景：待激活态接管输入 → 修补段内相机与画布 → 构建段 scope

        public async UniTask<bool> LoadSegmentAsync(string name_)
        {
            // 加载到待激活态，先不激活段场景，避免相机闪帧。
            AsyncOperation load = SceneManager.LoadSceneAsync(name_, LoadSceneMode.Additive);
            if (load == null)
            {
                Debug.LogError($"[{nameof(SegmentSceneLoader)}] 段场景 {name_} 不在构建列表，本段无法加载");
                return false;
            }

            load.allowSceneActivation = false;
            await UniTask.WaitUntil(() => load.progress >= 0.9f);

            load.allowSceneActivation = true;
            await load.ToUniTask();

            // 段已激活、根可访问：先记账，后面任何早退都要留得下可回收的段
            Scene newScene = SceneManager.GetSceneByName(name_);
            if (!newScene.IsValid())
            {
                Debug.LogError($"[{nameof(SegmentSceneLoader)}] 段场景 {name_} 激活后取不到句柄，本段无法收尾");
                return false;
            }

            _currentSegment = newScene;

            // 根节点数组只取一次：上一版每段流程各取一遍，重复分配同一份数组
            GameObject[] roots = newScene.GetRootGameObjects();

            // 关掉段内自带相机 / AudioListener（组件级，资产仍保留供编辑查看）
            foreach (GameObject root in roots)
            {
                foreach (Camera cam in root.GetComponentsInChildren<Camera>(true)) cam.enabled = false;
                foreach (AudioListener al in root.GetComponentsInChildren<AudioListener>(true)) al.enabled = false;
            }

            LifetimeScope segmentScope = null;
            foreach (GameObject root in roots)
            {
                LifetimeScope found = root.GetComponentInChildren<LifetimeScope>(true);
                if (found && found != _runScope)
                {
                    segmentScope = found;
                    break;
                }
            }

            if (segmentScope == null)
            {
                Debug.LogError($"[{nameof(SegmentSceneLoader)}] 段场景 {name_} 里没有 LifetimeScope，本段不会构建");
                return false;
            }

            if (!_camera)
                Debug.LogError($"[{nameof(SegmentSceneLoader)}] 常驻相机未注入，段场景 {name_} 的画布世界相机会留空");

            // 段内画布先补常驻相机（必须在 Build 之前：注入与卡池实例生成都在 Build 内、且会缓存 canvas.worldCamera）
            foreach (GameObject root in roots)
            foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
                canvas.worldCamera = _camera;

            // 跨场景父引用无法序列化，运行时赋值后再构建（各段 LifetimeScope 已关 autoRun）
            segmentScope.parentReference.Object = _runScope;
            segmentScope.Build();
            return true;
        }

        #endregion

        #region 卸载当前段：场景可能已被别处卸载，此时 UnloadSceneAsync 返回 null，直接跳过

        public async UniTask UnloadCurrentSegmentAsync()
        {
            try
            {
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