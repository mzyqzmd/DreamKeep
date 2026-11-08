using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>药水奖励面板就位：清单由域层掷出，视图只铺条目并在继续时交回确认。</summary>
    public struct EE_RewardPotionsOffered : IEventSegmentEvent
    {
        public string Title { get; }
        public IReadOnlyList<RunReward> Rewards { get; }

        public EE_RewardPotionsOffered(string title_, IReadOnlyList<RunReward> rewards_)
        {
            Title = title_;
            Rewards = rewards_;
        }
    }
}
