namespace _DreamKeep.GameRun
{
    /// <summary>存档里的段上下文：None 表示玩家已回到地图，Entering 表示停在段入口，AwaitingReward 表示战斗已胜利待领奖。</summary>
    public enum ESaveSegmentContext
    {
        None = 0,
        Entering = 1,
        AwaitingReward = 2,
    }
}
