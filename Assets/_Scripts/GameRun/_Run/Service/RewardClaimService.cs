using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>领奖会话：弹板前登记本次清单，界面只上报「第几条」与三选一选中的卡。
    /// 写域、幂等与待领清单清理都在这里；存档用的整批快照不动，读档仍按同一批重新领取。</summary>
    public sealed class RewardClaimService
    {
        private readonly RunState _run;
        private readonly HashSet<int> _claimed = new();
        private IReadOnlyList<RunReward> _rewards;

        public RewardClaimService(RunState run_) => _run = run_;

        #region 会话：弹板前登记、按序号领取、收尾清清单

        public void Begin(IReadOnlyList<RunReward> rewards_)
        {
            _rewards = rewards_;
            _claimed.Clear();
        }

        /// <summary>按登记顺序领取一条：重复领取返回 Stale；药水槽满返回 PotionSlotsFull 且不记为已领，条目可再点。</summary>
        public ERewardClaimResult Claim(int index_, CardModel selectedCard_ = null)
        {
            if (_rewards == null || index_ < 0 || index_ >= _rewards.Count) return ERewardClaimResult.Stale;
            if (_claimed.Contains(index_)) return ERewardClaimResult.Stale;

            RunReward reward = _rewards[index_];
            switch (reward.Kind)
            {
                case ERewardKind.Gold:
                    _run.GainGold(reward.Gold);
                    break;
                case ERewardKind.Card:
                    if (selectedCard_ == null) return ERewardClaimResult.ConfigMissing;
                    _run.Player.AddCard(selectedCard_.ToMutable());
                    break;
                case ERewardKind.Potion:
                    if (reward.Potion == null) return ERewardClaimResult.ConfigMissing;
                    if (!_run.Player.TryAddPotion(reward.Potion)) return ERewardClaimResult.PotionSlotsFull;
                    break;
                case ERewardKind.Relic:
                    if (reward.Relic == null) return ERewardClaimResult.ConfigMissing;
                    _run.Player.AddRelic(reward.Relic);
                    break;
                default:
                    return ERewardClaimResult.ConfigMissing;
            }

            _claimed.Add(index_);
            return ERewardClaimResult.Success;
        }

        public void End()
        {
            _rewards = null;
            _claimed.Clear();
            _run.ClearPendingRewards();
        }

        #endregion
    }
}
