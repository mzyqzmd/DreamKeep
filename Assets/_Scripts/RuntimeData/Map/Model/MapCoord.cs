namespace _DreamKeep.RuntimeData
{
    /// <summary>地图网格坐标：col 列 row 行，行号向上递增（起点在 row 0、Boss 在网格外）。</summary>
    public readonly struct MapCoord
    {
        public byte Col { get; }
        public byte Row { get; }

        public MapCoord(byte col_, byte row_)
        {
            Col = col_;
            Row = row_;
        }

        public override string ToString() => $"({Col},{Row})";
    }
}
