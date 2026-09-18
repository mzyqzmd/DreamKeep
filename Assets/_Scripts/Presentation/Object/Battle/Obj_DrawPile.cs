using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>抽牌堆视图：目前只提供抽牌起点锚点，抽牌入场演出从该点起飞。
    /// TODO 弃牌堆洗回与抽空的反馈（BE_NoCardCanDraw）、堆叠数量显示均未实现。</summary>
    public class Obj_DrawPile : MonoBehaviour
    {
        public Vector3 Anchor => transform.position;
    }
}
