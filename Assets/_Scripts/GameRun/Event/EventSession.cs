using System;
using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>事件段规则模型：掷事件、按条件生成选项视图模型、执行无需玩家再选的即时效果，并为自选效果掷出候选。
    /// 纯 C#，不认识场景与视图；随机流走整局 Rng，同种子可复现。</summary>
    public sealed class EventSession
    {
        private readonly RunState _run;
        private readonly SO_Event[] _events;

        public EventSession(RunState run_, SO_Event[] events_)
        {
            _run = run_;
            _events = events_;
        }

        public SO_Event PickEvent() => _events[_run.Rng.Next(_events.Length)];

        /// <summary>选项 → 视图模型：置灰的副标题换成原因文案；展示卡按配置烘焙成只读卡面。</summary>
        public List<EventOptionViewModel> BuildOptionVMs(IReadOnlyList<EventOptionData> options_)
        {
            List<EventOptionViewModel> vms = new(options_.Count);
            for (int i = 0; i < options_.Count; i++)
            {
                EventOptionData option = options_[i];
                bool enabled = IsEnabled(option, out string reason);
                CardModel card = option.DisplayCard != null ? CardModelBaker.Bake(option.DisplayCard) : null;
                vms.Add(new EventOptionViewModel(option.Name, enabled ? option.Subtitle : reason, enabled, card));
            }

            return vms;
        }

        /// <summary>遗物 N 选一候选 → 视图模型：标题取配置，副标题为遗物名与描述。</summary>
        public List<EventOptionViewModel> BuildRelicChoiceVMs(EED_RelicChoice effect_, IReadOnlyList<SO_Relic> picks_)
        {
            List<EventOptionViewModel> vms = new(picks_.Count);
            for (int i = 0; i < picks_.Count; i++)
                vms.Add(new EventOptionViewModel(effect_.PickTitle,
                    $"{picks_[i].RelicName}:{picks_[i].Description}", true, null));
            return vms;
        }

        /// <summary>执行不需要玩家再次选择的即时效果；返回 false 表示这是自选效果，由流程循环处理。</summary>
        public bool ApplyInstantEffect(EventEffectData effect_)
        {
            switch (effect_)
            {
                case EED_SpendGold spendGold:
                    _run.SpendGold(spendGold.Amount);
                    return true;
                case EED_GainGold gainGold:
                    _run.GainGold(gainGold.Amount);
                    return true;
                case EED_LoseHp loseHp:
                    _run.SpendPlayerHp(loseHp.Amount);
                    return true;
                case EED_RestoreHp restoreHp:
                    _run.RestorePlayerHp(restoreHp.Amount);
                    return true;
                case EED_AddCard addCard:
                    _run.Player.AddCard(CardModelBaker.Bake(addCard.Card).ToMutable());
                    return true;
                case EED_RandomRelic randomRelic:
                    SO_Relic relic = RelicRoller.RollOne(_run.Player, new[] { randomRelic.Pool }, _run.Rng);
                    if (relic != null) _run.Player.AddRelic(relic);
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>遗物 N 选一候选：抽取全交给 RelicRoller，池空自动降级到兜底池。</summary>
        public List<SO_Relic> RollRelicChoices(EED_RelicChoice effect_) =>
            RelicRoller.Roll(_run.Player, new[] { effect_.Pool }, effect_.Count, _run.Rng);

        /// <summary>药水奖励清单：从池里洗牌取指定瓶数，逐条转成奖励条目。</summary>
        public List<RunReward> RollPotions(EED_RewardPotions effect_)
        {
            List<SO_Potion> potions = new(effect_.Pool.Potions);
            List<SO_Potion> bottles = _run.Rng.TakeShuffled(potions, Math.Min(effect_.Count, potions.Count));
            List<RunReward> rewards = new(bottles.Count);
            for (int i = 0; i < bottles.Count; i++) rewards.Add(RunReward.PotionOf(bottles[i]));
            return rewards;
        }

        // 条件求值：不满足即置灰并给出原因（扣血后至少留 1 点，靠置灰而非封顶杜绝致死）
        private bool IsEnabled(EventOptionData option_, out string reason_)
        {
            switch (option_.Condition)
            {
                case EEventCondition.GoldAtLeast:
                    if (_run.Gold < option_.ConditionValue)
                    {
                        reason_ = "金币不足";
                        return false;
                    }

                    break;
                case EEventCondition.HpAbove:
                    if (_run.Player.Creature.CurrentHp <= option_.ConditionValue)
                    {
                        reason_ = "生命不足";
                        return false;
                    }

                    break;
                case EEventCondition.DeckAtLeast:
                    if (_run.Player.Deck.Count < option_.ConditionValue)
                    {
                        reason_ = "牌组不足";
                        return false;
                    }

                    break;
                case EEventCondition.Always:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(option_),
                        $"{option_.Condition} 未登记：漏登记会让条件不满足的选项照样可点");
            }

            reason_ = null;
            return true;
        }
    }
}
