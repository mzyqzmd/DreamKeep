using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>消耗堆视图：目前只提供归宿终点锚点，带消耗与遗忘标记的牌飞向该点并缩到 0 后回收。</summary>
    public class Obj_ExhaustPileView : MonoBehaviour
    {
        public Vector3 Anchor => transform.position;
    }
}
