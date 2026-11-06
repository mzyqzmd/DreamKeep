using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>先古之民段流程：名号、对话页、三选一与拾起时结算都在本域，等待点由命令门面放行，
    /// 玩法事实经先古段分发器播出。领取失败重开三选一；结算异常不吞收尾；
    /// 领取完成即标记节点通过并播段完成事件（本段没有「继续」按钮）。</summary>
    public sealed class AncientLoop
    {
        private readonly AncientSession _session;
        private readonly RunState _run;
        private readonly AncientEventDispatcher _events;
        private readonly RelicTriggerNotifier _notifier;
        private UniTaskCompletionSource<int> _choice = new();
        private UniTaskCompletionSource<IReadOnlyList<CardModel>> _cardSelection = new();

        public AncientLoop(AncientSession session_, RunState run_, AncientEventDispatcher events_,
            RelicTriggerNotifier notifier_)
        {
            _session = session_;
            _run = run_;
            _events = events_;
            _notifier = notifier_;
        }

        public void Start() => RunAsync().Forget();

        public void Choose(int index_) => _choice.TrySetResult(index_);

        public void SubmitCardSelection(IReadOnlyList<CardModel> cards_) => _cardSelection.TrySetResult(cards_);

        private async UniTask RunAsync()
        {
            // 候选全空时三选一会把三槽全隐藏、点击无从发生：直接报错收尾，别把玩家卡在段里
            if (_session.Choices.Count == 0)
            {
                await ReportAsync($"[{nameof(AncientLoop)}] 遗物池没有可用候选，本段直接收尾");
                _run.CompleteMapNode();
                await _events.PlayAsync(new AE_AncientCompleted());
                return;
            }

            SO_Ancient config = _session.Config;
            await _events.PlayAsync(new AE_NameplateBound(config.AncientName, config.PositionName));
            AncientDialoguePage page = _session.CurrentPage;
            if (page != null)
                await _events.PlayAsync(new AE_DialoguePageOffered(page, config.AncientView, config.PlayerView));

            // 可用性只算一次：三选一期间生命值不会变化，重开也是同一份
            List<bool> claimable = new(_session.Choices.Count);
            for (int i = 0; i < _session.Choices.Count; i++) claimable.Add(_session.CanClaim(i));

            while (true)
            {
                await _events.PlayAsync(new AE_ChoicesOffered(_session.Choices, claimable));
                int index = await TakeChoiceAsync();
                if (!_session.Claim(index))
                {
                    await ReportAsync($"[{nameof(AncientLoop)}] 遗物入列失败（候选下标 {index}），已重开三选一");
                    continue;
                }

                SO_Relic relic = _session.Choices[index];
                await _events.PlayAsync(new AE_ChoicesHidden());
                _notifier.NotifyTriggered(relic); // 领到即播一次涟漪（与休息段挖掘同口径）
                try
                {
                    await ResolvePickupAsync(relic);
                }
                catch (Exception e)
                {
                    // 结算异常不能连带吞掉收尾：遗物已入列、代价可能已付，段必须照常开地图
                    await ReportAsync($"[{nameof(AncientLoop)}] {relic.name} 的拾起时结算异常：{e}");
                }

                _run.CompleteMapNode(); // 遗物领取完成即通过（起点开局已预置，幂等）
                break;
            }

            await _events.PlayAsync(new AE_AncientCompleted());
        }

        // 拾起时结算：一次性效果的唯一落点，按资产类型分派（与 RelicBaker 里的 null 豁免成对）；
        // 扣血代价的置灰已在选项上挡了一道，真选中时仍保底留 1 绝不致死
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
                    await PickCardsAsync(new AE_UpgradeCardsRequested(pomander.CardCount));
                    break;
                case SO_PrecariousShears shears:
                    _run.SpendPlayerHp(relic_.PickupHpCost);
                    await PickCardsAsync(new AE_RemoveCardsRequested(shears.CardCount));
                    break;
                case SO_ArcaneScroll scroll:
                    await AddRandomCard(scroll.CardPool);
                    break;
                case SO_SmallCapsule capsule:
                    AddRandomRelic(capsule.Pools);
                    break;
                case SO_Circlet:
                    break; // 头环：兜底占位，无拾起时效果
                default:
                    await ReportAsync($"[{nameof(AncientLoop)}] {relic_.name} 的拾起时效果未登记");
                    break;
            }
        }

        // 强制选牌：取消许可由表现层关掉；万一仍交回取消就重弹，玩家不能空手离场
        private async UniTask PickCardsAsync(IAncientEvent request_)
        {
            while (true)
            {
                await _events.PlayAsync(request_);
                if (await TakeCardSelectionAsync() != null) return;
            }
        }

        // 随机金卡入牌库：烘焙出的原型必须克隆成局内卡才能进牌组；断引用先滤掉，Bake(null) 会当场 NRE
        private async UniTask AddRandomCard(SO_CardPool pool_)
        {
            List<SO_Card> candidates = new();
            if (pool_ != null)
                for (int i = 0; i < pool_.Cards.Count; i++)
                    if (pool_.Cards[i] != null) candidates.Add(pool_.Cards[i]);

            if (candidates.Count == 0)
            {
                await ReportAsync($"[{nameof(AncientLoop)}] 随机金卡没有可用候选（卡池为空或接线缺失）");
                return;
            }

            _run.Player.AddCard(CardModelBaker.Bake(_run.Rng.Pick(candidates)).ToMutable());
        }

        // 随机遗物：抽取全交给 RelicRoller，池空自动降级到兜底池
        private void AddRandomRelic(IReadOnlyList<SO_RelicPool> pools_)
        {
            SO_Relic relic = RelicRoller.RollOne(_run, pools_, _run.Rng);
            if (relic == null) return;
            _run.Player.AddRelic(relic);
        }

        private UniTask ReportAsync(string message_) => _events.PlayAsync(new AE_ErrorReported(message_));

        private async UniTask<int> TakeChoiceAsync()
        {
            int index = await _choice.Task;
            _choice = new UniTaskCompletionSource<int>();
            return index;
        }

        private async UniTask<IReadOnlyList<CardModel>> TakeCardSelectionAsync()
        {
            IReadOnlyList<CardModel> cards = await _cardSelection.Task;
            _cardSelection = new UniTaskCompletionSource<IReadOnlyList<CardModel>>();
            return cards;
        }
    }
}
