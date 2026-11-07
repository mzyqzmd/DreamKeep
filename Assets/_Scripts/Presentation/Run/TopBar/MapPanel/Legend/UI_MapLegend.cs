using UnityEngine;
using _DreamKeep.ConfigData;

namespace _DreamKeep.Presentation
{
    /// <summary>地图图例：中转条目的悬停进出——亮起/还原地图上对应展示类型的节点，条目类型序列化在 UI_MapLegendItem。</summary>
    public class UI_MapLegend : MonoBehaviour
    {
        [Tooltip("地图视图（高亮执行者）")] [SerializeField]
        private UI_MapView _view;

        /// <summary>条目悬停变化：开＝点亮该类型节点，关＝还原。</summary>
        public void SetHover(EMapPointType type_, bool on_)
        {
            if (_view) _view.HighlightType(type_, on_);
        }
    }
}
