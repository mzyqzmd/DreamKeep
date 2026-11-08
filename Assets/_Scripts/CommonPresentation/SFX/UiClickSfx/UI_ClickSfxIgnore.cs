using UnityEngine;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>点击音静音标记：挂在任意节点上即让该子树内的按钮不播点击音（拦截器沿父链查）。
    /// 供「是按钮但不需要点击反馈」的整屏关闭热区之类使用，纯标记、零逻辑。</summary>
    public sealed class UI_ClickSfxIgnore : MonoBehaviour
    {
    }
}
