namespace _DreamKeep.ConfigData
{
    /// <summary>事件选项的置灰条件：不满足时选项可见但不可点；置灰原因文案由表现层按条件类型生成。</summary>
    public enum EEventCondition
    {
        Always, // 永远可选
        GoldAtLeast, // 金币 ≥ 阈值
        HpAbove, // 当前生命 > 阈值（扣血后至少留 1 点）
        DeckAtLeast, // 牌组张数 ≥ 阈值
    }
}
