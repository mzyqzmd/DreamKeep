using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace _DreamKeep.Presentation
{
    /// <summary>段场景装卸器：Additive 加载段场景并装配段 scope，卸载时回收场景句柄。
    /// 加载到待激活态时先临时关常驻 EventSystem，激活后关段内相机 / EventSystem / AudioListener、
    /// 给段内 Canvas 补常驻主相机（必须在 Build 之前），最后恢复常驻 EventSystem。
    /// 只负责装载，不决定何时进段、何时收段。</summary>
    public sealed class SegmentSceneLoader
    {
        private readonly Camera _camera;
        private readonly RunLifetimeScope _runScope;
        private Scene _currentSegment;

        public SegmentSceneLoader(Camera camera_, RunLifetimeScope runScope_)
        {
            _camera = camera_;
            _runScope = runScope_;
        }

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
                if (!newScene.IsValid())
                {
                    Debug.LogError($"[{nameof(SegmentSceneLoader)}] 段场景 {name_} 激活后取不到句柄，本段无法收尾");
                    return false;
                }

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

                if (segmentScope == null)
                {
                    Debug.LogError($"[{nameof(SegmentSceneLoader)}] 段场景 {name_} 里没有 LifetimeScope，本段不会构建");
                    return false;
                }

                // 段内画布先补常驻相机（必须在 Build 之前：注入与卡池实例生成都在 Build 内、且会缓存 canvas.worldCamera）
                foreach (GameObject root in newScene.GetRootGameObjects())
                foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
                    canvas.worldCamera = _camera;

                // 跨场景父引用无法序列化，运行时赋值后再构建（各段 LifetimeScope 已关 autoRun）
                segmentScope.parentReference.Object = _runScope;
                segmentScope.Build();
                return true;
            }
            finally
            {
                // 常驻 EventSystem 只在这里恢复：早退与异常都不许让整局 UI 失去输入
                if (resident) resident.enabled = true;
            }
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
