namespace _DreamKeep.Presentation
{
    /// <summary>说明气泡的触发模式：Pointer = 组件自己接 UGUI 指针事件（悬停即弹、移出即收）；
    /// External = 不响应指针，由调用方在它判定出的时刻显式调 Show()/Hide()（手牌卡牌的悬停事实由手牌区裁决，即这一种）。</summary>
    public enum ETipTrigger
    {
        Pointer,
        External,
    }
}
