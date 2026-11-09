namespace _DreamKeep.RuntimeData
{
    /// <summary>类型配额：固定语义行之外，各随机类型在本章的目标数量，由生成配置掷高斯得出。</summary>
    public class MapPointTypeCounts
    {
        public int NumOfElites { get; }
        public int NumOfShops { get; }
        public int NumOfUnknowns { get; }
        public int NumOfRests { get; }

        public MapPointTypeCounts(int elites_, int shops_, int unknowns_, int rests_)
        {
            NumOfElites = elites_;
            NumOfShops = shops_;
            NumOfUnknowns = unknowns_;
            NumOfRests = rests_;
        }
    }
}
