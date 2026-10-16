namespace _DreamKeep.RuntimeData
{
    /// <summary>牌堆分类：给"哪一堆"一个与具体字段解耦的名字，供战斗态映射与跨层查询使用。</summary>
    public enum EPileKind
    {
        Draw,
        Hand,
        Discard,
        Exhaust,
        Play,
    }
}
