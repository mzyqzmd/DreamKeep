using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>休息段流程：选项组装、条件求值与四条结算分支都在本域，等待点由命令门面放行，玩法事实经休息段分发器播出。
    /// 弹板取消即回到选项重选；任一路在不可撤销点标记节点通过，「继续」每次点击都播一次段完成。</summary>
    public sealed class RestLoop
    {
        private readonly RunState _run;
        private readonly List<SO_RestOption> _restOptions;
        private readonly SO_RelicPool _commonPool;
        private readonly SO_RelicPool _rarePool;
        private readonly RelicTriggerNotifier _notifier;
        private readonly RestEventDispatcher _events;
        private UniTaskCompletionSource<int> _choice = new();
        private UniTaskCompletionSource<IReadOnlyList<CardModel>> _cardSelection = new();
        private UniTaskCompletionSource _proceed = new();

        public RestLoop(RunState run_, List<SO_RestOption> restOptions_, SO_RelicPool commonPool_,
            SO_RelicPool rarePool_, RelicTriggerNotifier notifier_, RestEventDispatcher events_)
        {
            _run = run_;
            _restOptions = restOptions_;
            _commonPool = commonPool_;
            _rarePool = rarePool_;
            _notifier = notifier_;
            _events = events_;
        }

        public void Start() => RunAsync().Forget();

        public void Choose(int index_) => _choice.TrySetResult(index_);

        public void Proceed() => _proceed.TrySetResult();

        public void SubmitCardSelection(IReadOnlyList<CardModel> cards_) => _cardSelection.TrySetResult(cards_);

        private async UniTask RunAsync()
        {
            SO_MeatCleaver cleaver = _run.Player.Relics.OfType<SO_MeatCleaver>().FirstOrDefault();
            List<SO_Relic> digCandidates = RelicRoller.Collect(_run, new[] { _commonPool, _rarePool });

            while (true)
            {
                List<SO_RestOption> shown = BuildOptions(cleaver, digCandidates, out RestOption[] options);
                await _events.PlayAsync(new REE_OptionsOffered(options));
                SO_RestOption chosen = shown[await TakeChoiceAsync()];
                await _events.PlayAsync(new REE_OptionsHidden());

                if (chosen is SO_HealRestOption heal)
                {
                    _run.RestorePlayerHp(HealAmount(_run.Player.Creature.MaxHp, heal.HealMaxHpRatio));
                    await _events.PlayAsync(new REE_RestSmokeRequested());
                    _run.CompleteMapNode();
                    break;
                }

                if (chosen is SO_ForgeRestOption)
                {
                    await _events.PlayAsync(new REE_UpgradeCardsRequested(1));
                    if (await TakeCardSelectionAsync() == null) continue;
                    _run.CompleteMapNode();
                    break;
                }

                if (chosen is SO_CookRestOption)
                {
                    await _events.PlayAsync(new REE_RemoveCardsRequested(cleaver.RemoveCards));
                    if (await TakeCardSelectionAsync() == null) continue;
                    _run.Player.Creature.RaiseMaxHp(cleaver.HpGain);
                    _run.CompleteMapNode();
                    break;
                }

                // 挖掘（组合根白名单保证只剩这一种类型）：先领取补遗物栏槽位，再播涟漪
                SO_Relic dug = _run.Rng.Pick(digCandidates);
                _run.Player.AddRelic(dug);
                _run.CompleteMapNode();
                _notifier.NotifyTriggered(dug);
                break;
            }

            await _events.PlayAsync(new REE_ProceedOffered());
            while (true)
            {
                await _proceed.Task;
                _proceed = new UniTaskCompletionSource();
                await _events.PlayAsync(new REE_RestCompleted());
            }
        }

        // 按配置列表组装选项：持有切肉刀/铲子才放行烹饪/挖掘；说明静态照抄资产，
        // 动态选项只算数值带出（域不出富文本标记，文案组装归 UI_RestOptionGroup）
        private List<SO_RestOption> BuildOptions(SO_MeatCleaver cleaver_, List<SO_Relic> digCandidates_,
            out RestOption[] options_)
        {
            List<SO_RestOption> configs = new(_restOptions.Count);
            List<RestOption> opts = new(_restOptions.Count);
            bool hasShovel = _run.Player.Relics.Any(static r => r is SO_Shovel);

            for (int i = 0; i < _restOptions.Count; i++)
            {
                SO_RestOption option = _restOptions[i];
                if (option is SO_CookRestOption && cleaver_ == null) continue;
                if (option is SO_DigRestOption && !hasShovel) continue;

                bool enabled;
                float? healRatio = null;
                int? healAmount = null;
                int? removeCards = null;
                int? hpGain = null;
                if (option is SO_HealRestOption heal)
                {
                    enabled = true;
                    healRatio = heal.HealMaxHpRatio;
                    healAmount = HealAmount(_run.Player.Creature.MaxHp, heal.HealMaxHpRatio);
                }
                else if (option is SO_ForgeRestOption)
                {
                    enabled = _run.Player.Deck.Cards.Any(static c => c.CanUpgrade);
                }
                else if (option is SO_CookRestOption)
                {
                    enabled = _run.Player.Deck.Count >= cleaver_.RemoveCards;
                    removeCards = cleaver_.RemoveCards;
                    hpGain = cleaver_.HpGain;
                }
                else
                {
                    enabled = digCandidates_.Count > 0;
                }

                opts.Add(new RestOption(option.OptionName, option.IsStaticDescription ? option.Description : null,
                    option.Icon, enabled, healRatio, healAmount, removeCards, hpGain));
                configs.Add(option);
            }

            options_ = opts.ToArray();
            return configs;
        }

        // 回血量：按最大生命百分比向上取整——结算（RunAsync）与文案数值（BuildOptions）共用一份换算
        private static int HealAmount(int maxHp_, float ratio_) => (int)Math.Ceiling(maxHp_ * ratio_);

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
