using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.GameRun;

namespace _DreamKeep.GameRun
{
    /// <summary>宝箱段模型：一次开启＝从 Common+Rare 双池合并候选里随机抽一件遗物、领取入列。
    /// 纯 C#，不认识引擎与容器；掷骰走整局随机流，同种子可复现。一局可多次遇宝箱，每次独立重掷。</summary>
    public sealed class ChestSession
    {
        /// <summary>本段抽中的遗物：构造时从双池合并候选里洗牌取一件；候选全空（已持有或池未配）时为 null。</summary>
        public SO_Relic Reward { get; }

        private readonly RunState _run;

        public ChestSession(SO_RelicPool commonPool_, SO_RelicPool rarePool_, RunState run_)
        {
            _run = run_;
            Reward = RollReward(commonPool_, rarePool_);
        }

        /// <summary>本段是否已领取：领取一次后守卫关闭，复访开新段会重掷。</summary>
        private bool Claimed { get; set; }

        #region 领取：守卫通过后遗物入列并标记本段已领取

        /// <summary>领取抽中的遗物：重复领取或遗物为空返回 false，入列失败也返回 false 且不标记。</summary>
        public bool Claim()
        {
            if (Claimed || Reward == null) return false;
            if (!_run.Player.AddRelic(Reward)) return false;
            Claimed = true;
            return true;
        }

        #endregion

        // 掷奖励：RelicRoller 合并双池候选，等权取一件；候选全空返回 null
        private SO_Relic RollReward(SO_RelicPool commonPool_, SO_RelicPool rarePool_) =>
            _run.Rng.Pick(RelicRoller.CollectCandidates(_run.Player, new[] { commonPool_, rarePool_ }));
    }
}
