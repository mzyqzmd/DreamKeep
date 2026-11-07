using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _DreamKeep.Presentation
{
    /// <summary>地图滚轮转发：图例/工具栏/纸面外空白都不在 ScrollRect 子树内，滚轮事件沿祖先链冒泡到本面板时
    /// 转交给地图滚动视图；纸面内的滚轮先命中 ScrollRect 自身并终止冒泡，不会被二次转发。</summary>
    public class UI_MapScrollForward : MonoBehaviour, IScrollHandler
    {
        [Tooltip("地图滚动视图（MapView 上的 ScrollRect）")] [SerializeField]
        private ScrollRect _scroll;

        public void OnScroll(PointerEventData eventData_)
        {
            if (_scroll) _scroll.OnScroll(eventData_);
        }
    }
}
