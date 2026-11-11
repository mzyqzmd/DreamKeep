using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>整局级命令入口：常驻界面只上报意图并拿回完成源，写局内数据与效果结算都由域侧执行。
    /// 只做同步转发，不持有事件通道——全局刷新仍由 RunState / Player 的原生通知下行。</summary>
    public sealed class RunCommandDispatcher
    {
        private readonly RunState _run;
        private readonly RewardClaimService _rewards;

        public RunCommandDispatcher(RunState run_, RewardClaimService rewards_)
        {
            _run = run_;
            _rewards = rewards_;
        }

        #region 整局计时：顶栏每帧上报真实时间增量

        public void Tick(double seconds_) => _run.TickTime(seconds_);

        #endregion

        #region 战斗外药水：先问能否结算再扣槽，失败时药水留在槽里

        /// <summary>战斗外使用一瓶药水：槽越界、空槽、不可战斗外结算都返回 false，槽位保持不变。</summary>
        public bool UsePotionOutsideBattle(int slotIndex_)
        {
            SO_Potion potion = PotionAt(slotIndex_);
            if (potion == null || !potion.UsableOutsideBattle) return false;
            if (!PotionResolver.TryApplyOutsideBattle(potion, _run.Player)) return false;
            _run.Player.TryTakePotion(slotIndex_, out _);
            return true;
        }

        /// <summary>战斗外丢弃：取走即算丢掉，槽位刷新由 PotionSlotChanged 通知驱动。</summary>
        public bool DiscardPotionOutsideBattle(int slotIndex_) => _run.Player.TryTakePotion(slotIndex_, out _);

        // 槽位读取与界面共用同一份数据真源，越界给 null
        private SO_Potion PotionAt(int slotIndex_)
        {
            IReadOnlyList<SO_Potion> potions = _run.Player.PotionSlots;
            return slotIndex_ >= 0 && slotIndex_ < potions.Count ? potions[slotIndex_] : null;
        }

        #endregion

        #region 领奖：弹板前登记清单，界面只交序号与三选一选中的卡

        public void BeginRewardClaim(IReadOnlyList<RunReward> rewards_) => _rewards.Begin(rewards_);

        public ERewardClaimResult ClaimReward(int index_, CardModel selectedCard_ = null) =>
            _rewards.Claim(index_, selectedCard_);

        public void EndRewardClaim() => _rewards.End();

        #endregion
    }
}
