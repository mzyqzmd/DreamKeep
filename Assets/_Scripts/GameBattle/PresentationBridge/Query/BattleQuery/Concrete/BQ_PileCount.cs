using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>查询「某牌堆当前有几张牌」：只携带牌堆分类这一个参数，答案由查询门面按域侧映射现取；
    /// 调用方（牌堆视图）各缓存一份复用，故本类不可变。</summary>
    public sealed class BQ_PileCount
    {
        public BQ_PileCount(EPileKind kind_) => Kind = kind_;

        /// <summary>要问的牌堆分类。</summary>
        public EPileKind Kind { get; }
    }
}
