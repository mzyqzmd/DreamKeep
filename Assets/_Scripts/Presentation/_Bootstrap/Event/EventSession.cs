using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using _DreamKeep.CommonPresentation;
using _DreamKeep.ConfigData;
using _DreamKeep.GameRun;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Presentation
{
    /// <summary>事件段编排器：掷一则事件、驱动两页流程（第 1 页抉择 → 选项效果 → 第 2 页结果 → 等待「继续」）。
    /// 条件求值与效果执行都在这里分派（配置层只存数据）；自选面板返回取消即写回血量/金币快照并重开第 1 页；
    /// 奖励发放口径：药水必走奖励面板、金币/单件遗物等单项直接执行、卡三选一直接弹三选一面板（本批未用）。</summary>
    public sealed class EventSession
    {
        private readonly RunState _run;
        private readonly SO_Event[] _events;
        private readonly UI_EventPanel _panel;
        private readonly UI_EventOptionGroup _options;
        private readonly UI_RewardPanel _rewardPanel;
        private readonly UI_CardRemovePanel _removePanel;
        private readonly UI_CardUpgradePanel _upgradePanel;

        public EventSession(RunState run_, SO_Event[] events_, UI_EventPanel panel_, UI_EventOptionGroup options_,
            UI_RewardPanel rewardPanel_, UI_CardRemovePanel removePanel_, UI_CardUpgradePanel upgradePanel_)
        {
            _run = run_;
            _events = events_;
            _panel = panel_;
            _options = options_;
            _rewardPanel = rewardPanel_;
            _removePanel = removePanel_;
            _upgradePanel = upgradePanel_;
        }

        #region 对外：整段事件流程

        /// <summary>掷一则事件并走完两页流程；返回时结果页与「继续」按钮已就位，
        /// 点「继续」后的收段与重复开地图归调用方（按钮保留、可反复点）。</summary>
        public async UniTask PlayAsync()
        {
            SO_Event config = PickEvent();
            _panel.Bind(config);

            EventOptionData chosen = await ChooseOptionAsync(config);
            _panel.ShowResult(chosen.ResultText);

            _options.Show(Array.Empty<EventOptionViewModel>(), true);
        }

        #endregion

        // 循环呈现第 1 页选项直到玩家完成一次不取消的选择；自选面板取消即写回快照重选
        private async UniTask<EventOptionData> ChooseOptionAsync(SO_Event config_)
        {
            while (true)
            {
                _options.Show(BuildOptionVMs(config_.Options), false);
                int index = await _options.WaitChoiceAsync();
                EventOptionData option = config_.Options[index];

                int hpSnapshot = _run.Player.Creature.CurrentHp;
                int goldSnapshot = _run.Gold;
                if (!await RunEffectsAsync(option.Effects)) return option;

                // 玩家在自选面板取消了：写回血量与金币快照，重开第 1 页选项（删牌面板取消时未动牌组，无需回滚）
                _run.Player.Creature.CurrentHp = hpSnapshot;
                _run.Gold = goldSnapshot;
            }
        }

        // 选项 → 视图模型：置灰的副标题换成原因文案；展示卡按配置烘焙成只读卡面
        private List<EventOptionViewModel> BuildOptionVMs(IReadOnlyList<EventOptionData> options_)
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

        // 效果执行：按配置顺序串行，任一自选面板取消即整体中止
        // 返回 true = 玩家取消了（交调用方回滚快照）
        private async UniTask<bool> RunEffectsAsync(IReadOnlyList<EventEffectData> effects_)
        {
            for (int i = 0; i < effects_.Count; i++)
                if (await RunEffectAsync(effects_[i])) return true;
            return false;
        }

        private UniTask<bool> RunEffectAsync(EventEffectData effect_) => effect_ switch
        {
            EED_SpendGold spendGold => SpendGold(spendGold),
            EED_GainGold gainGold => GainGold(gainGold),
            EED_LoseHp loseHp => LoseHp(loseHp),
            EED_RestoreHp restoreHp => RestoreHp(restoreHp),
            EED_AddCard addCard => AddCard(addCard),
            EED_RemoveCards removeCards => RemoveCardsAsync(removeCards),
            EED_UpgradeCards upgradeCards => UpgradeCardsAsync(upgradeCards),
            EED_RandomRelic randomRelic => RandomRelic(randomRelic),
            EED_RelicChoice relicChoice => RelicChoiceAsync(relicChoice),
            EED_RewardPotions rewardPotions => RewardPotionsAsync(rewardPotions),
            _ => throw new ArgumentOutOfRangeException(nameof(effect_),
                $"{effect_.GetType().Name} 未登记：漏登记会让该效果点了没反应"),
        };

        // 支出金币：金额不足已由选项条件置灰兜住，这里仍钳到不为负（配置失误时与条件口径一致）
        private UniTask<bool> SpendGold(EED_SpendGold effect_)
        {
            _run.Gold = Mathf.Max(0, _run.Gold - effect_.Amount);
            return UniTask.FromResult(false);
        }

        // 获得金币：单项目奖励直接入账（RunState.GainGold）
        private UniTask<bool> GainGold(EED_GainGold effect_)
        {
            _run.GainGold(effect_.Amount);
            return UniTask.FromResult(false);
        }

        // 失去生命：当前生命严格大于扣血值才可选（HpAbove），这里仍保底留 1 点，绝不致死（对齐先古拾取代价口径）
        private UniTask<bool> LoseHp(EED_LoseHp effect_)
        {
            Creature creature = _run.Player.Creature;
            creature.CurrentHp = Mathf.Max(1, creature.CurrentHp - effect_.Amount);
            return UniTask.FromResult(false);
        }

        // 恢复生命：钳到上限，满血时白选（选项永远可选的保底出口）
        private UniTask<bool> RestoreHp(EED_RestoreHp effect_)
        {
            Creature creature = _run.Player.Creature;
            creature.CurrentHp = Mathf.Min(creature.CurrentHp + effect_.Amount, creature.MaxHp);
            return UniTask.FromResult(false);
        }

        // 加入卡牌：配置卡烘焙成局内卡（ToMutable 后）追加进牌组
        private UniTask<bool> AddCard(EED_AddCard effect_)
        {
            _run.Player.AddCard(CardModelBaker.Bake(effect_.Card).ToMutable());
            return UniTask.FromResult(false);
        }

        // 自选移除：面板自管选牌、删除与演出；关闭键交还 null = 取消
        private async UniTask<bool> RemoveCardsAsync(EED_RemoveCards effect_)
        {
            _removePanel.Show(_run.Player, effect_.Count);
            IReadOnlyList<CardModel> removed = await _removePanel.WaitResultAsync();
            return removed == null;
        }

        // 自选升级：一次把请求张数交给面板（实际目标由面板钳到可升级卡数）；可升级卡为 0 时面板照样弹出并允许
        // 直接确认完成，不让「什么也升不了」把事件流程卡住；关闭键取消
        private async UniTask<bool> UpgradeCardsAsync(EED_UpgradeCards effect_)
        {
            _upgradePanel.Show(_run.Player, effect_.Count);
            IReadOnlyList<CardModel> upgraded = await _upgradePanel.WaitResultAsync();
            return upgraded == null;
        }

        // 单件随机遗物：过滤已持有后随机一件直接入包；候选为空则空转
        private UniTask<bool> RandomRelic(EED_RandomRelic effect_)
        {
            List<SO_Relic> candidates = RelicCandidates(effect_.Pool);
            if (candidates.Count > 0) _run.Player.AddRelic(_run.Rng.Pick(candidates));
            return UniTask.FromResult(false);
        }

        // 遗物 N 选一（E08 二段式）：选项组刷新为「选择这个」子选项，点选一件直接入包；候选不足按实数、为空则空转
        private async UniTask<bool> RelicChoiceAsync(EED_RelicChoice effect_)
        {
            List<SO_Relic> candidates = RelicCandidates(effect_.Pool);
            int count = Mathf.Min(effect_.Count, candidates.Count);
            if (count == 0) return false;

            List<SO_Relic> picks = _run.Rng.TakeShuffled(candidates, count);
            List<EventOptionViewModel> vms = new(picks.Count);
            for (int i = 0; i < picks.Count; i++)
                vms.Add(new EventOptionViewModel(effect_.PickTitle,
                    $"{picks[i].RelicName}:{picks[i].Description}", true, null));
            _options.Show(vms, false);
            int index = await _options.WaitChoiceAsync();
            _run.Player.AddRelic(picks[index]);
            return false;
        }

        // 药水奖励：无论几瓶都弹奖励面板逐条领取（槽满条目解锁让玩家先喝再领）；「继续」只关面板回到事件
        private async UniTask<bool> RewardPotionsAsync(EED_RewardPotions effect_)
        {
            List<SO_Potion> potions = new(effect_.Pool.Potions);
            List<SO_Potion> bottles = _run.Rng.TakeShuffled(potions, Mathf.Min(effect_.Count, potions.Count));
            List<RunReward> rewards = new(bottles.Count);
            for (int i = 0; i < bottles.Count; i++) rewards.Add(RunReward.PotionOf(bottles[i]));

            UniTaskCompletionSource done = new();
            _rewardPanel.Show(effect_.PanelTitle, rewards, () => done.TrySetResult());
            await done.Task;
            _rewardPanel.Close();
            return false;
        }

        // 遗物池候选：过滤已持有（候选为空的极端态由调用方空转兜底）
        private List<SO_Relic> RelicCandidates(SO_RelicPool pool_)
        {
            List<SO_Relic> candidates = new();
            IReadOnlyList<SO_Relic> relics = pool_.Relics;
            for (int i = 0; i < relics.Count; i++)
                if (!_run.Player.HasRelic(relics[i])) candidates.Add(relics[i]);
            return candidates;
        }

        // 从本批已实装事件里随机掷一则（同源整局随机流；池分层与去重等跑图地图落地后再接）
        private SO_Event PickEvent() => _events[_run.Rng.Next(_events.Length)];
    }
}
