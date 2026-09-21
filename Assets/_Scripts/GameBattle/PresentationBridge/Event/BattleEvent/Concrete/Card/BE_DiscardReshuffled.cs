namespace _DreamKeep.GameBattle
{
    /// <summary>弃牌堆洗回抽牌堆：抽牌堆见底时整摞弃牌移入并洗牌，载荷为洗回张数，供视图播出"牌从弃牌堆搬进抽牌堆"的短演出。</summary>
    public struct BE_DiscardReshuffled : IBattleEvent
    {
        public int Count { get; }

        public BE_DiscardReshuffled(int count_) => Count = count_;
    }
}
