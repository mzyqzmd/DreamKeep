namespace _DreamKeep.EngineUtility
{
    /// <summary>整局进行态：区分同一节点下的三种阶段，读档据此决定重进战斗、重领奖励还是回到地图。</summary>
    public enum ESavePhase
    {
        InBattle,
        AwaitingReward,
        OnMap
    }
}
