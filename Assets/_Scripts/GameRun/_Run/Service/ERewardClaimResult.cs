namespace _DreamKeep.GameRun
{
    /// <summary>一次奖励兑现的结果：界面据此决定条目收起，还是解锁让玩家处理后重试。</summary>
    public enum ERewardClaimResult
    {
        Success,
        PotionSlotsFull,
        ConfigMissing,
        Stale,
    }
}
