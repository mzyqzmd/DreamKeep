using System.Collections.Generic;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>地图节点视觉配置：节点类型 → 图标/描边 与 Unknown 揭示变体的映射，全部数值由资产配置。</summary>
    [CreateAssetMenu(fileName = "MapNodeVisual", menuName = "DreamKeep/Data/Map/节点视觉")]
    public class SO_MapNodeVisual : ScriptableObject
    {
        [SerializeField] private List<MapNodeVisualEntry> _entries = new();

        [Tooltip("Unknown 节点揭示后的变体：与常规条目同构，键填揭示出的类型")] [SerializeField]
        private List<MapNodeVisualEntry> _unknownVariants = new();

        #region 查询：按类型取常规视觉、按揭示类型取变体视觉

        public bool TryGetVisual(EMapPointType type_, out Sprite icon_, out Sprite outline_)
        {
            MapNodeVisualEntry entry = FindEntry(type_);
            icon_ = entry?.Icon;
            outline_ = entry?.Outline;
            return entry != null && icon_ != null;
        }

        public bool TryGetVariant(EMapPointType type_, EMapPointType revealedType_, out Sprite icon_,
            out Sprite outline_)
        {
            icon_ = null;
            outline_ = null;
            if (type_ != EMapPointType.Event) return false; // 只有 Unknown（"?"）节点有揭示变体
            MapNodeVisualEntry variant = _unknownVariants.Find(v_ => v_.Type == revealedType_);
            if (variant == null) return false;
            icon_ = variant.Icon;
            outline_ = variant.Outline;
            return icon_ != null;
        }

        private MapNodeVisualEntry FindEntry(EMapPointType type_) =>
            _entries.Find(e_ => e_.Type == type_);

        #endregion
    }
}