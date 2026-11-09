using UnityEngine;
using UnityEngine.EventSystems;
using _DreamKeep.ConfigData;

namespace _DreamKeep.Presentation
{
    /// <summary>地图图例条目：序列化所代表的节点类型，悬停进出上报 UI_MapLegend；
    /// 面板收起时 Exit 不会来，OnDisable 兜底解除高亮。</summary>
    public class UI_MapLegendItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("所代表的节点类型（与节点展示类型对号，\"?\"条目填 Event）")] [SerializeField]
        private EMapPointType _type;

        private UI_MapLegend _legend;

        // 面板装配期是 inactive 不跑 Awake：图例惰性取，且必须连 inactive 父链一起查（先例 UI_MapLine.EnsureRefs）
        private UI_MapLegend Legend => _legend != null ? _legend : _legend = GetComponentInParent<UI_MapLegend>(true);

        public void OnPointerEnter(PointerEventData _) => Legend?.SetHover(_type, true);
        public void OnPointerExit(PointerEventData _) => Legend?.SetHover(_type, false);

        private void OnDisable() => Legend?.SetHover(_type, false);
    }
}
