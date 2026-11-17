namespace _DreamKeep.ConfigData
{
    /// <summary>选牌词条的作用档位：AllHand 作用于全部手牌；Exact 指定必须选满的张数；UpTo 指定至多可选的张数；
    /// Any 表示任选若干张，忽略张数配置（上限取候选数）。</summary>
    public enum ESelectScope
    {
        AllHand,
        Exact,
        UpTo,
        Any,
    }
}
