using UnityEngine;

namespace _DreamKeep.EngineUtility
{
    /// <summary>UGUI 组件自愈小工具：取既有或补挂，供"预制体没挂也能跑"的兜底装配。</summary>
    public static class UiCanvas
    {
        /// <summary>返回节点上已有的 CanvasGroup，没有则补挂一个（照回合横幅先例的自愈口径）。</summary>
        public static CanvasGroup EnsureCanvasGroup(Component host_)
        {
            if (host_.TryGetComponent<CanvasGroup>(out CanvasGroup group)) return group;
            return host_.gameObject.AddComponent<CanvasGroup>();
        }
    }
}
