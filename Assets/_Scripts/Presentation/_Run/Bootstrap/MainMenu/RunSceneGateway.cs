using System.Threading;
using _DreamKeep.CommonPresentation;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;
using _DreamKeep.EngineUtility;

namespace _DreamKeep.Presentation
{
    /// <summary>整局场景网关：additive 装载/卸载 Scene_Run、手动 Build 整局容器、查找整局 scope，
    /// 并把常驻服务在 Build 前转交给整局组合根。不认识主菜单按钮、弹层与存档时序。</summary>
    public sealed class RunSceneGateway
    {
        private readonly string _sceneName;
        private readonly Camera _camera;
        private readonly AudioRoot _audio;
        private readonly ScreenShakeDriver _shakeDriver;
        private readonly string _logPrefix;
        private Scene _scene;

        public RunSceneGateway(string sceneName_, Camera camera_, AudioRoot audio_, ScreenShakeDriver shakeDriver_,
            string logPrefix_)
        {
            _sceneName = sceneName_;
            _camera = camera_;
            _audio = audio_;
            _shakeDriver = shakeDriver_;
            _logPrefix = logPrefix_;
        }

        public bool IsValid => _scene.IsValid() && _scene.isLoaded;

        /// <summary>装载并修补整局场景，确认场景内有整局 scope 才算成功。</summary>
        public async UniTask<bool> LoadAsync(CancellationToken destructionToken_)
        {
            _scene = await SceneLoadUtil.LoadPreparedAsync(_sceneName, _camera, destructionToken_);
            if (!_scene.IsValid())
            {
                Debug.LogError($"[{_logPrefix}] 场景 {_sceneName} 加载失败（不在构建列表或激活后取不到句柄），无法进局");
                return false;
            }
            if (SceneLoadUtil.FindScope(_scene) == null)
            {
                Debug.LogError($"[{_logPrefix}] 场景 {_sceneName} 里没有 LifetimeScope，整局容器不会构建");
                return false;
            }
            return true;
        }

        /// <summary>就地手动 Build 整局容器：autoRun 关闭，Build 前转交常驻服务。</summary>
        public void BuildScope()
        {
            LifetimeScope scope = SceneLoadUtil.FindScope(_scene);
            if (scope == null)
            {
                Debug.LogError($"[{_logPrefix}] 场景 {_sceneName} 里没有 LifetimeScope，整局容器不会构建");
                return;
            }

            if (scope is RunLifetimeScope runScope) runScope.BindPersistentServices(_audio, _shakeDriver);
            else Debug.LogError($"[{_logPrefix}] 整局 scope 不是 {nameof(RunLifetimeScope)}，常驻服务无法转交");
            scope.Build();
        }

        /// <summary>卸载整局场景并清句柄。</summary>
        public async UniTask UnloadAsync() => _scene = await SceneLoadUtil.UnloadAsync(_scene);
    }
}
