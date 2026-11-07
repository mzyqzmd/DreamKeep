using System.Collections.Generic;

namespace _DreamKeep.GameRun
{
    /// <summary>整章地图的磁盘形态：节点扁平成表，起点 / Boss / 当前节点 / 已通过集合一律用表下标表示。
    /// Boss 位于网格外的虚拟行，靠 BossIndex 单独记录，不落进网格重建。</summary>
    public sealed class SaveMap
    {
        public int Width { get; set; }
        public int Rows { get; set; }
        public List<SaveMapNode> Nodes { get; set; } = new();
        public int StartIndex { get; set; }
        public int BossIndex { get; set; }
        public int CurrentIndex { get; set; } = -1;
        public List<int> ClearedIndices { get; set; } = new();
        public bool SegmentPending { get; set; }
    }
}
