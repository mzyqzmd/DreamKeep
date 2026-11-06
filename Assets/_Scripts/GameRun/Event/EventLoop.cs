using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>事件段流程：等待点由命令门面放行，玩法事实经事件段分发器播出；纯 C#，不认识场景与视图。
    /// 自选面板取消即写回血量与金币快照并重开第 1 页；结果页弹出当帧标记节点通过，
    /// 「继续」每次点击都播一次段完成。</summary>
    public sealed class EventLoop
    {
        private readonly EventSession _session;
        private readonly RunState _run;
        private readonly EventEventDispatcher _events;
        private UniTaskCompletionSource<int> _choice = new();
        private UniTaskCompletionSource<IReadOnlyList<CardModel>> _cardSelection = new();
        private UniTaskCompletionSource _rewardAck = new();
        private UniTaskCompletionSource _proceed = new();

        public EventLoop(EventSession session_, RunState run_, EventEventDispatcher events_)
        {
            _session = session_;
            _run = run_;
            _events = events_;
        }

        public void Start() => RunAsync().Forget();

        public void Choose(int index_) => _choice.TrySetResult(index_);

        public void Proceed() => _proceed.TrySetResult();

        public void SubmitCardSelection(IReadOnlyList<CardModel> cards_) => _cardSelection.TrySetResult(cards_);

        public void AcknowledgeReward() => _rewardAck.TrySetResult();

        // 等待点由构造期建好：命令早于 await 到达也不会丢；循环型等待点在 await 之后重置
        private async UniTask RunAsync()
        {
            SO_Event config = _session.PickEvent();
            await _events.PlayAsync(new EE_EventOpened(config));
            EventOptionData chosen = await ChooseOptionAsync(config);
            await _events.PlayAsync(new EE_ResultShown(chosen.ResultText));
            _run.CompleteMapNode(); // 结果页弹出即通过，不等玩家点「继续」
            await _events.PlayAsync(new EE_OptionsOffered(Array.Empty<EventOptionViewModel>(), true));

            while (true)
            {
                await _proceed.Task;
                _proceed = new UniTaskCompletionSource();
                await _events.PlayAsync(new EE_EventCompleted());
            }
        }

        // 循环呈现第 1 页选项直到玩家完成一次不取消的选择
        private async UniTask<EventOptionData> ChooseOptionAsync(SO_Event config_)
        {
            while (true)
            {
                await _events.PlayAsync(new EE_OptionsOffered(_session.BuildOptionVMs(config_.Options), false));
                EventOptionData option = config_.Options[await TakeChoiceAsync()];
                int hpSnapshot = _run.Player.Creature.CurrentHp;
                int goldSnapshot = _run.Gold;
                if (!await RunEffectsAsync(option.Effects)) return option;

                // 玩家在自选面板取消了：回滚血量与金币快照，重开第 1 页选项（删牌面板取消时未动牌组，无需回滚）
                _run.RestorePlayerHpTo(hpSnapshot);
                _run.RestoreGoldTo(goldSnapshot);
            }
        }

        // 效果按配置顺序串行；任一自选取消即整体中止，交调用方回滚快照
        private async UniTask<bool> RunEffectsAsync(IReadOnlyList<EventEffectData> effects_)
        {
            for (int i = 0; i < effects_.Count; i++)
            {
                EventEffectData effect = effects_[i];
                if (_session.ApplyInstantEffect(effect)) continue;
                if (await RunInteractiveEffectAsync(effect)) return true;
            }

            return false;
        }

        private async UniTask<bool> RunInteractiveEffectAsync(EventEffectData effect_)
        {
            switch (effect_)
            {
                case EED_RemoveCards removeCards:
                    await _events.PlayAsync(new EE_RemoveCardsRequested(removeCards.Count));
                    return await TakeCardSelectionAsync() == null;
                case EED_UpgradeCards upgradeCards:
                    await _events.PlayAsync(new EE_UpgradeCardsRequested(upgradeCards.Count));
                    return await TakeCardSelectionAsync() == null;
                case EED_RelicChoice relicChoice:
                    List<SO_Relic> picks = _session.RollRelicChoices(relicChoice);
                    if (picks.Count == 0) return false; // 兜底未接线时退化为不发放
                    await _events.PlayAsync(new EE_OptionsOffered(
                        _session.BuildRelicChoiceVMs(relicChoice, picks), false));
                    _session.ClaimRelicChoice(picks[await TakeChoiceAsync()]);
                    return false;
                case EED_RewardPotions rewardPotions:
                    await _events.PlayAsync(new EE_RewardPotionsOffered(rewardPotions.PanelTitle,
                        _session.RollPotions(rewardPotions)));
                    await _rewardAck.Task;
                    _rewardAck = new UniTaskCompletionSource();
                    return false;
                default:
                    throw new ArgumentOutOfRangeException(nameof(effect_),
                        $"{effect_.GetType().Name} 未登记：漏登记会让该效果点了没反应");
            }
        }

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
