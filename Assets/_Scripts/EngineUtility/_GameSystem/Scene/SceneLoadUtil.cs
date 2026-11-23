using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace _DreamKeep.EngineUtility
{
    /// <summary>Additive 场景装载与回收：待激活加载 → 激活取句柄 → 场景修补 / 卸载 / 作用域查找。
    /// 只碰 SceneManager 与场景内的相机/画布/LifetimeScope，不认识任何游戏类型，
    /// 所以 Presentation 与 CommonPresentation 两侧能共用同一条场景序列。</summary>
    public static class SceneLoadUtil
    {
        #region 装载并修补一个 additive 场景

        /// <summary>加载 additive 场景并完成通用修补：等 progress ≥ 0.9 再激活（避免相机闪帧），
        /// 关掉场景自带的相机与 AudioListener（组件级，资产保留供编辑查看），给全部画布补常驻相机
        /// （必须在容器 Build 之前：装配期实例会缓存 canvas.worldCamera）。
        /// 取消时已发起但未走完的场景一律回收，不留半载僵尸场景；返回 default(Scene) 表示失败或取消，由调用方按自己的前缀报错。</summary>
        public static async UniTask<Scene> LoadPreparedAsync(string sceneName_, Camera canvasCamera_,
            CancellationToken cancellationToken_)
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(sceneName_, LoadSceneMode.Additive);
            if (load == null) return default;

            Scene scene = default;
            try
            {
                // 先不激活：半载时就激活会闪一帧空相机
                load.allowSceneActivation = false;
                if (await UniTask.WaitUntil(() => load.progress >= 0.9f, cancellationToken: cancellationToken_)
                        .SuppressCancellationThrow())
                    return default;

                load.allowSceneActivation = true;
                if (await load.ToUniTask(cancellationToken: cancellationToken_).SuppressCancellationThrow())
                    return default;

                scene = SceneManager.GetSceneByName(sceneName_);
                if (!scene.IsValid()) return default;

                // 根节点数组只取一次：每段流程各取一遍会重复分配同一份数组
                GameObject[] roots = scene.GetRootGameObjects();
                foreach (GameObject root in roots)
                {
                    foreach (Camera cam in root.GetComponentsInChildren<Camera>(true)) cam.enabled = false;
                    foreach (AudioListener listener in root.GetComponentsInChildren<AudioListener>(true)) listener.enabled = false;
                }

                if (!canvasCamera_) return scene; // 常驻相机未注入：画布世界相机留空，由调用方报错
                foreach (GameObject root in roots)
                foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
                    canvas.worldCamera = canvasCamera_;
                return scene;
            }
            finally
            {
                // 取消 / 异常时场景可能停在 90% 未激活：Unity 不允许卸载未激活的 additive 场景，
                // 先放行让它走完再卸。句柄此时尚未交给调用方，本工具全权回收，避免半载僵尸场景累积。
                if (!scene.IsValid() && cancellationToken_.IsCancellationRequested)
                    await RecoverCancelledLoadAsync(sceneName_, load);
            }
        }

        // 取消兜底：停在 90% 的 additive 场景先激活走完，再卸载；IsValid 检查防止重复卸载
        private static async UniTask RecoverCancelledLoadAsync(string sceneName_, AsyncOperation load_)
        {
            load_.allowSceneActivation = true;
            await load_.ToUniTask();
            Scene stray = SceneManager.GetSceneByName(sceneName_);
            if (stray.IsValid() && stray.isLoaded)
            {
                AsyncOperation unload = SceneManager.UnloadSceneAsync(stray);
                if (unload != null) await unload.ToUniTask();
            }
        }

        #endregion

        #region 卸载一个 additive 场景

        /// <summary>卸载场景并返回已清空的句柄：句柄可能已失效（外部卸载）此时 UnloadSceneAsync 返回 null，直接跳过。
        /// 调用方用 `scene = await UnloadAsync(scene)` 接收，保证异常路径也不残留失效 Scene。</summary>
        public static async UniTask<Scene> UnloadAsync(Scene scene_)
        {
            try
            {
                if (scene_.IsValid() && scene_.isLoaded)
                    await SceneManager.UnloadSceneAsync(scene_.name).ToUniTask();
            }
            finally
            {
                scene_ = default;
            }
            return scene_;
        }

        #endregion

        #region 在场景内查找容器作用域

        /// <summary>按深度优先在场景根节点下找第一个 LifetimeScope；except_ 非空时跳过它（段场景排除整局 scope）。
        /// 找不到返回 null，由调用方按自己的前缀报错。</summary>
        public static LifetimeScope FindScope(Scene scene_, LifetimeScope except_ = null)
        {
            foreach (GameObject root in scene_.GetRootGameObjects())
            {
                LifetimeScope found = root.GetComponentInChildren<LifetimeScope>(true);
                if (found && found != except_) return found;
            }
            return null;
        }

        #endregion
    }
}
