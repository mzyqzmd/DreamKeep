using UnityEngine;
using VContainer;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>悬停气泡注入工具：把场景内所有 UI_HoverTip（含失活对象）补注入当前容器，
    /// 供各组合根在构建回调里一行调用。无状态，重复注入只是重赋依赖，无副作用。</summary>
    public static class HoverTipBinder
    {
        public static void InjectAll(IObjectResolver resolver_)
        {
            foreach (UI_HoverTip tip in Object.FindObjectsOfType<UI_HoverTip>(true))
                resolver_.Inject(tip);
        }
    }
}
