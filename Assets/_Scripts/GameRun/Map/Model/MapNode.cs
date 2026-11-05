using System.Collections.Generic;
using _DreamKeep.ConfigData;

namespace _DreamKeep.GameRun
{
    /// <summary>地图节点：网格坐标 + 类型 + 父子边（双向由 AddChildPoint/RemoveChildPoint 维护）。
    /// 可达性状态由 MapGraph 重算；Unknown 节点进入后由流程写 RevealedType，供 UI 换变体图标。</summary>
    public class MapNode
    {
        /// <summary>网格坐标：生成期间可被布局后处理平移（同程序集），生成后只读消费。</summary>
        public MapCoord Coord { get; internal set; }

        public EMapPointType PointType { get; internal set; }

        /// <summary>固定语义行与锚点为假：生成器的类型改写与剪枝不得碰它们。</summary>
        public bool CanBeModified { get; internal set; } = true;

        public List<MapNode> Parents { get; } = new();
        public List<MapNode> Children { get; } = new();

        public EMapNodeState State { get; internal set; } = EMapNodeState.Untravelable;

        /// <summary>Unknown 节点揭示后的实际内容类型；None = 未揭示。</summary>
        public EMapPointType RevealedType { get; internal set; } = EMapPointType.None;

        /// <summary>切段与遭遇判定用的有效类型：问号揭示后取揭示结果，其余节点即自身类型。</summary>
        public EMapPointType ResolvedType => RevealedType != EMapPointType.None ? RevealedType : PointType;

        public MapNode(MapCoord coord_, EMapPointType type_ = EMapPointType.None)
        {
            Coord = coord_;
            PointType = type_;
        }

        #region 双向边维护：加边去重、断边两处同步

        public void AddChildPoint(MapNode child_)
        {
            if (Children.Contains(child_)) return;
            Children.Add(child_);
            child_.Parents.Add(this);
        }

        public void RemoveChildPoint(MapNode child_)
        {
            if (!Children.Remove(child_)) return;
            child_.Parents.Remove(this);
        }

        #endregion
    }
}