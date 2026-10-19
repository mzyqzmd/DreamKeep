namespace _DreamKeep.Presentation
{
    /// <summary>数字飘字的两种表现形态：向上缓慢飘升渐隐 / 斜向上抛出后受重力下坠。</summary>
    public enum ENumberPopTipType
    {
        Rise, // 弹出后沿竖直方向缓慢飘升，全程渐隐
        Toss, // 弹出后向斜上方抛出，受重力下坠，到时长后回收
    }
}
