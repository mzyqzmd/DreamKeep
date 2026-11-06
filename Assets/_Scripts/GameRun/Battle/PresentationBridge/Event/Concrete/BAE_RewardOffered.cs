using System.Collections.Generic;

namespace _DreamKeep.GameRun
{
    /// <summary>胜利且掷出非空奖励：视图弹奖励面板逐条领取，「继续」经命令门面交回领完意图。</summary>
    public struct BAE_RewardOffered : IBattleSegmentEvent
    {
        public IReadOnlyList<RunReward> Rewards { get; }

        public BAE_RewardOffered(IReadOnlyList<RunReward> rewards_) => Rewards = rewards_;
    }
}
