using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.GameRun;

namespace _DreamKeep.Presentation
{
    /// <summary>先古之民段模型：一次拜访＝随机抽一段对话、掷出三件候选遗物、守卫领取一件。
    /// 纯 C#，不认识引擎与容器；掷骰走整局随机流，同种子可复现。
    /// 一局内只会遇到一次，跨局的拜访次数记录等存档系统落地后再加。</summary>
    public sealed class AncientSession
    {
        /// <summary>三选一：候选件数恒为三，候选不足时按实数出。</summary>
        private const int ChoiceCount = 3;

        private readonly SO_Ancient _config;
        private readonly RunState _run;
        private readonly AncientDialoguePage _page;
        private readonly List<SO_Relic> _choices = new();

        public AncientSession(SO_Ancient config_, RunState run_)
        {
            _config = config_;
            _run = run_;
            _page = _config.Pages.Count == 0 ? null : _config.Pages[_run.Rng.Next(0, _config.Pages.Count)];
            RollChoices();
        }

        /// <summary>本段先古之民的配置：表现层取展示名与说话人外观用。</summary>
        public SO_Ancient Config => _config;

        /// <summary>本次拜访的对话页：构造时从全部页中随机掷定，未配置对话返回 null。</summary>
        public AncientDialoguePage CurrentPage => _page;

        /// <summary>三选一的候选遗物：进段时掷定（跳过已持有与断引用，不足三件按实数出）。</summary>
        public IReadOnlyList<SO_Relic> Choices => _choices;

        /// <summary>本段是否已领取：领取一次后整段收尾，复访会开新段重掷。</summary>
        private bool Claimed { get; set; }

        /// <summary>该候选能否领取：生命值不足以支付拾取代价（扣完至少留 1 点）时置灰用；越界返回 false。</summary>
        public bool CanClaim(int choiceIndex_) =>
            choiceIndex_ >= 0 && choiceIndex_ < _choices.Count &&
            _run.Player.Creature.CurrentHp - _choices[choiceIndex_].PickupHpCost >= 1;

        #region 领取：守卫通过后遗物入列并标记本段已领取

        /// <summary>领取下标对应的候选遗物：重复领取或下标非法返回 false，入列失败也返回 false 且不标记。</summary>
        public bool Claim(int choiceIndex_)
        {
            if (Claimed || choiceIndex_ < 0 || choiceIndex_ >= _choices.Count) return false;
            if (!_run.Player.AddRelic(_choices[choiceIndex_])) return false;
            Claimed = true;
            return true;
        }

        #endregion

        // 掷候选：收集池内有效且未持有的遗物，再局部洗牌无放回取前三（照 EncounterRewardRoller 口径）
        private void RollChoices()
        {
            List<SO_Relic> candidates = new();
            SO_RelicPool pool = _config.RelicPool;
            if (pool != null)
                foreach (SO_Relic relic in pool.Relics)
                    if (relic != null && !_run.Player.HasRelic(relic))
                        candidates.Add(relic);

            _choices.AddRange(_run.Rng.TakeShuffled(candidates, ChoiceCount));
        }
    }
}
