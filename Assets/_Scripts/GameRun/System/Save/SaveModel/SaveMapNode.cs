using System.Collections.Generic;
using _DreamKeep.ConfigData;

namespace _DreamKeep.GameRun
{
    /// <summary>一个地图节点的磁盘形态：子节点按下标存，避免 Parents / Children 双向引用成环；可达态由 MapGraph 重算不进档。</summary>
    public sealed class SaveMapNode
    {
        public int Col { get; set; }
        public int Row { get; set; }
        public EMapPointType PointType { get; set; }
        public EMapPointType RevealedType { get; set; }
        public bool CanBeModified { get; set; }
        public List<int> Children { get; set; } = new();
    }
}
