namespace _DreamKeep.ConfigData
{
    /// <summary>药水落点：决定使用流程要不要先选目标，也决定效果作用于谁。
    /// 刻意不复用 ECardTargetType——那是「卡」的语义，且多出随机与最前敌人两个本批用不到的值。</summary>
    public enum EPotionTargetType
    {
        /// <summary>自身：点「使用」即刻生效。</summary>
        Self,

        /// <summary>单体敌人：进入待选态，由玩家指向一名存活敌人。</summary>
        AnyEnemy,

        /// <summary>全体敌人：点「使用」即刻对全部存活敌人生效。</summary>
        AllEnemy,
    }
}
