using System.Collections.Generic;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>地图节点视觉配置：节点类型 → 图标/描边/悬停说明 与 Unknown 揭示变体的映射，全部数值由资产配置。</summary>
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

        /// <summary>节点展示视觉：揭示变体优先（仅"?"节点），回落常规条目；地图节点与层栏共用。</summary>
        public bool TryGetDisplayVisual(EMapPointType pointType_, EMapPointType revealedType_, out Sprite icon_,
            out Sprite outline_)
        {
            if (revealedType_ != EMapPointType.None &&
                TryGetVariant(pointType_, revealedType_, out icon_, out outline_))
                return true;
            return TryGetVisual(pointType_, out icon_, out outline_);
        }

        /// <summary>节点悬停说明：标题与描述，恒读常规条目（变体表只管图标）。</summary>
        public bool TryGetInfo(EMapPointType type_, out string name_, out string description_)
        {
            MapNodeVisualEntry entry = FindEntry(type_);
            name_ = entry?.Name;
            description_ = entry?.Description;
            return entry != null && !string.IsNullOrEmpty(description_);
        }

        private bool TryGetVariant(EMapPointType type_, EMapPointType revealedType_, out Sprite icon_,
            out Sprite outline_)
        {
            icon_ = null;
            outline_ = null;
            if (type_ != EMapPointType.Unknown) return false; // 只有 Unknown（"?"）节点有揭示变体
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