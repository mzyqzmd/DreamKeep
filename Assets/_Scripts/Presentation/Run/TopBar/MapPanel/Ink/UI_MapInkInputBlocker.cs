using UnityEngine;
using UnityEngine.EventSystems;

namespace _DreamKeep.Presentation
{
    /// <summary>工具模式期间的左键拦截层：吃掉按下/拖拽事件，阻止冒泡到 ScrollRect 把地图拖走；
    /// 绘制与擦除由 UI_MapInk 轮询输入完成，滚轮不在此拦截（不实现 IScrollHandler，仍可滚图）。</summary>
    public class UI_MapInkInputBlocker : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public void OnPointerDown(PointerEventData _) { }
        public void OnPointerUp(PointerEventData _) { }
        public void OnBeginDrag(PointerEventData _) { }
        public void OnDrag(PointerEventData _) { }
        public void OnEndDrag(PointerEventData _) { }
    }
}
