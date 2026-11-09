using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;
using _DreamKeep.EngineUtility;

namespace _DreamKeep.Presentation
{
    /// <summary>段场景装卸器：Additive 加载段场景并装配段 scope，卸载时回收场景句柄。
    /// 装载序列（待激活加载 → 激活 → 关相机/AudioListener → 补画布相机）统一走 <see cref="SceneLoadUtil"/>，
    /// 本类只负责"段"这一侧：认段 scope、把父引用指向整局 scope、再 Build。
    /// 常驻相机跨场景不可序列化，由编排者运行时注入，因此本类自带 BindCamera 而不是构造期固定。
    /// 只负责装载，不决定何时进段、何时收段。</summary>
    public sealed class SegmentSceneLoader
    {
        private readonly RunLifetimeScope _runScope;
        private Scene _currentSegment;
        private Camera _camera;

        public SegmentSceneLoader(RunLifetimeScope runScope_) => _runScope = runScope_;

        /// <summary>注入常驻主相机：段内画布与段 scope 构建都要用它，未注入时按装配错误报一条。</summary>
        public void BindCamera(Camera camera_) => _camera = camera_;

        #region 装载段场景：装载并修补 → 认段 scope → 指向整局 scope → Build

        public async UniTask<bool> LoadSegmentAsync(string name_)
        {
            if (!_camera)
                Debug.LogError($"[{nameof(SegmentSceneLoader)}] 常驻相机未注入，段场景 {name_} 的画布世界相机会留空");

            Scene scene = await SceneLoadUtil.LoadPreparedAsync(name_, _camera, _runScope.destroyCancellationToken);
            if (!scene.IsValid())
            {
                Debug.LogError($"[{nameof(SegmentSceneLoader)}] 段场景 {name_} 加载失败（不在构建列表或激活后取不到句柄），本段无法加载");
                return false;
            }

            // 段已激活、根可访问：先记账，后面任何早退都要留得下可回收的段
            _currentSegment = scene;

            LifetimeScope segmentScope = null;
            foreach (GameObject root in scene.GetRootGameObjects())
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
