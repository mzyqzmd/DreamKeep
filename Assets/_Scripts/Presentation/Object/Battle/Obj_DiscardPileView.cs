using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>弃牌堆视图：目前只提供归宿终点锚点，离场的牌飞向该点并缩到 0 后回收。</summary>
    public class Obj_DiscardPileView : MonoBehaviour
    {
        public Vector3 Anchor => transform.position;
    }
}
