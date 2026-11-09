using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _DreamKeep.EngineUtility
{
    /// <summary>Additive 场景装载：待激活加载 → 激活取句柄 → 场景修补。
    /// 只碰 SceneManager 与场景内的相机/画布，不认识任何容器——LifetimeScope 的查找与 Build 一律留调用方，
    /// 所以 Presentation 与 CommonPresentation 两侧能共用同一条装载序列。</summary>
    public static class SceneLoadUtil
    {
        #region 装载并修补一个 additive 场景

        /// <summary>加载 additive 场景并完成通用修补：等 progress ≥ 0.9 再激活（避免相机闪帧），
        /// 关掉场景自带的相机与 AudioListener（组件级，资产保留供编辑查看），给全部画布补常驻相机
        /// （必须在容器 Build 之前：装配期实例会缓存 canvas.worldCamera）。
        /// 场景不在构建列表、或激活后取不到句柄时返回 default(Scene)，由调用方按自己的前缀报错。</summary>
        public static async UniTask<Scene> LoadPreparedAsync(string sceneName_, Camera canvasCamera_,
            CancellationToken cancellationToken_)
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(sceneName_, LoadSceneMode.Additive);
            if (load == null) return default;

            // 先不激活：半载时就激活会闪一帧空相机
            load.allowSceneActivation = false;
            await UniTask.WaitUntil(() => load.progress >= 0.9f, cancellationToken: cancellationToken_);
            load.allowSceneActivation = true;
            await load.ToUniTask(cancellationToken: cancellationToken_);

            Scene scene = SceneManager.GetSceneByName(sceneName_);
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

        #endregion
    }
}
