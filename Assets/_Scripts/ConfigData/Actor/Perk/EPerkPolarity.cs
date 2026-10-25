namespace _DreamKeep.ConfigData
{
    /// <summary>状态极性：只决定面向玩家的呈现（施加演出里名称是增益色还是减益色），不参与任何玩法结算。
    /// 默认值 Buff 是刻意的：新状态漏填只会显示成绿色，不会把一个增益错标成红。</summary>
    public enum EPerkPolarity
    {
        /// <summary>增益：对宿主有利。</summary>
        Buff,

        /// <summary>减益：对宿主不利。</summary>
        Debuff,
    }
}
