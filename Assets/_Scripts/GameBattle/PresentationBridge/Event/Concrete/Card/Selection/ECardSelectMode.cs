namespace _DreamKeep.GameBattle
{
    /// <summary>手牌选择阶段的类型：升级陈列升级后副本，消耗陈列待消耗的原卡；
    /// 是否必须选满由同请求上的 ESelectScope 决定，本枚举只管陈列形态。</summary>
    public enum ECardSelectMode
    {
        Upgrade,
        Exhaust,
    }
}
