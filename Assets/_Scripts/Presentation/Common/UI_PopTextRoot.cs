using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>弹层挂载点标记：挂在场景 Canvas_Pop/PopTextRoot 上，供 UI_InfoPanel 等弹层生成方按类型
    /// FindFirstObjectByType 查找（预制体持不了场景引用，也不做字符串查找）；场景里没有该标记时由生成方自行回退自身所在 Canvas。</summary>
    public class UI_PopTextRoot : MonoBehaviour
    {
    }
}
