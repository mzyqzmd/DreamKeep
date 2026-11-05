using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;
using _DreamKeep.CommonPresentation;
using _DreamKeep.ConfigData;
using _DreamKeep.GameRun;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Presentation
{
    /// <summary>先古之民段入口：本段"从哪里开始"的唯一落点——名号横幅 → 对话演绎 → 三选一 → 拾起时结算 → 开地图并保留本段。
    /// 拾起时效果是局外效果（RelicBaker 里登记为 null），与休息段处理铲子/切肉刀同构，结算就落在本类里；
    /// 领取失败（候选已失效等装配外异常）只报错并重开三选一，不吞不崩。</summary>
    public sealed class AncientEntry : IStartable
    {
        private readonly AncientSession _session;
        private readonly UI_AncientDialoguePanel _panel;
        private readonly UI_AncientOptionGroup _options;
        private readonly UI_AncientPopRoot _popRoot;
        private readonly SegmentSceneManager _segmentManager;
        private readonly RunState _run;
        private readonly RelicTriggerNotifier _notifier;
        private readonly UI_CardUpgradePanel _upgradePanel;
        private readonly UI_CardRemovePanel _removePanel;

        public AncientEntry(AncientSession session_, UI_AncientDialoguePanel panel_, UI_AncientOptionGroup options_,
            UI_AncientPopRoot popRoot_, SegmentSceneManager segmentManager_, RunState run_,
            RelicTriggerNotifier notifier_, UI_CardUpgradePanel upgradePanel_, UI_CardRemovePanel removePanel_)
        {
            _session = session_;
            _panel = panel_;
            _options = options_;
            _popRoot = popRoot_;
            _segmentManager = segmentManager_;
            _run = run_;
            _notifier = notifier_;
            _upgradePanel = upgradePanel_;
            _removePanel = removePanel_;
        }

        public void Start() => PlayAsync().Forget();

        private async UniTask PlayAsync()
        {
            // 池空（未接线或全持有）时三选一会把三槽全隐藏、点击无从发生——直接报错收尾，别把玩家卡在段里
            if (_session.Choices.Count == 0)
            {
                Debug.LogError($"[{nameof(AncientEntry)}] 遗物池没有可用候选，本段直接收尾");
                _run.CompleteMapNode(); // 兜底解除段挂起并幂等标记通过（起点在图构造时已预置已通过），保证池空也能正常收尾开图
                _segmentManager.CompleteSegment();
                return;
            }

            SO_Ancient config = _session.Config;
            _panel.BindNameplate(config.AncientName, config.PositionName);
            _popRoot.PlayBanner(config.AncientName, config.PositionName);

            AncientDialoguePage page = _session.CurrentPage;
            if (page != null)
                await _panel.PlayPageAsync(page, config.AncientView, config.PlayerView);

            // 可用性只算一次：三选一期间生命值不会变化，重开也是同一份
            List<bool> claimable = new(_session.Choices.Count);
            for (int i = 0; i < _session.Choices.Count; i++) claimable.Add(_session.CanClaim(i));

            while (true)
            {
                _options.Show(_session.Choices, claimable);
                int index = await _options.WaitChoiceAsync();
                if (!_session.Claim(index))
                {
                    Debug.LogError($"[{nameof(AncientEntry)}] 遗物入列失败（候选下标 {index}），已重开三选一");
                    continue;
                }

                SO_Relic relic = _session.Choices[index];
                _options.Hide();
                _notifier.NotifyTriggered(relic); // 领到即播一次涟漪（与休息段挖掘同口径）
                try
                {
                    await ResolvePickupAsync(relic);
                }
                catch (Exception e)
                {
                    // 结算异常不能连带吞掉收尾：遗物已入列、代价可能已付，段必须照常开地图
                    Debug.LogError($"[{nameof(AncientEntry)}] {relic.name} 的拾起时结算异常：{e}");
                }

                _run.CompleteMapNode(); // 遗物领取完成即通过（起点开局已预置，幂等）
                break;
            }

            _segmentManager.CompleteSegment();
        }

        // 拾起时结算：一次性效果的唯一落点，按资产类型分派（与 RelicBaker 里的 null 豁免成对）；
        // 扣血代价的置灰已在选项上挡了一道，真选中时仍保底留 1 绝不致死（口径收在 RunState.SpendPlayerHp）
        private async UniTask ResolvePickupAsync(SO_Relic relic_)
        {
            switch (relic_)
            {
                case SO_GoldenPearl pearl:
                    _run.GainGold(pearl.Gold);
                    break;
                case SO_NutritiousOyster oyster:
                    _run.Player.Creature.RaiseMaxHp(oyster.MaxHpGain);
                    break;
                case SO_Pomander pomander:
                    _run.SpendPlayerHp(relic_.PickupHpCost);
                    await PickCardsAsync(_upgradePanel, pomander.CardCount);
                    break;
                case SO_PrecariousShears shears:
                    _run.SpendPlayerHp(relic_.PickupHpCost);
                    await PickCardsAsync(_removePanel, shears.CardCount);
                    break;
                case SO_ArcaneScroll scroll:
                    AddRandomCard(scroll.CardPool);
                    break;
                case SO_SmallCapsule capsule:
                    AddRandomRelic(capsule.Pools);
                    break;
                default:
                    Debug.LogError($"[{nameof(AncientEntry)}] {relic_.name} 的拾起时效果未登记");
                    break;
            }
        }

        // 强制选牌：Show 内部的清场会把取消许可复位，故 Show 之后再关掉；万一仍返回取消就重弹，玩家不能空手离场
        private async UniTask PickCardsAsync(UI_CardSelectPanel panel_, int count_)
        {
            while (true)
            {
                panel_.Show(_run.Player, count_);
                panel_.SetCancelable(false);
                if (await panel_.WaitResultAsync() != null) return;
            }
        }

        // 随机金卡入牌库：烘焙出的原型必须克隆成局内卡才能进牌组；断引用先滤掉，Bake(null) 会当场 NRE
        private void AddRandomCard(SO_CardPool pool_)
        {
            List<SO_Card> candidates = new();
            if (pool_ != null)
                for (int i = 0; i < pool_.Cards.Count; i++)
                    if (pool_.Cards[i] != null) candidates.Add(pool_.Cards[i]);

            if (candidates.Count == 0)
            {
                Debug.LogError($"[{nameof(AncientEntry)}] 随机金卡没有可用候选（卡池为空或接线缺失）");
                return;
            }

            _run.Player.AddCard(CardModelBaker.Bake(_run.Rng.Pick(candidates)).ToMutable());
        }

        // 随机遗物：多池合并等权抽一件（RelicRoller 收集口径）
        private void AddRandomRelic(IReadOnlyList<SO_RelicPool> pools_)
        {
            List<SO_Relic> candidates = RelicRoller.CollectCandidates(_run.Player, pools_);
            if (candidates.Count == 0)
            {
                Debug.LogError($"[{nameof(AncientEntry)}] 随机遗物没有可发放的候选（池空或已全部持有）");
                return;
            }

            _run.Player.AddRelic(_run.Rng.Pick(candidates));
        }
    }
}
