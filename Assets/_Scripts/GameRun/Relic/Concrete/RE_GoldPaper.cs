using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>金纸：每消耗 N 张牌抽牌，计数整局累计、不按回合重置、跨战斗保留（住 Player 计数表）。</summary>
    public sealed class RE_GoldPaper : RelicEffect_T<SO_GoldPaper>
    {
        public RE_GoldPaper(SO_GoldPaper data_, BattleContext context_, ActionSystem actions_, BattleEventDispatcher dispatcher_,
            RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            Dispatcher.Register<BE_CardExhausted>(OnCardExhausted);
            NotifyCount(Context.Player.RelicCounterOf(Relic)); // 进战斗显示上场保留的计数值
        }

        private async UniTask OnCardExhausted(BE_CardExhausted ev_)
        {
            int count = Context.Player.AdvanceRelicCounter(Relic);
            if (count < Data.Count) { NotifyCount(count); return; }
            Context.Player.ResetRelicCounter(Relic);
            NotifyCount(0);
            Creature self = Context.Player.Creature;
            List<Creature> target = PlayerTarget;
            BattleAction[] draws = new BattleAction[Data.Cards];
            for (int i = 0; i < draws.Length; i++) draws[i] = new BA_DrawCard(self, target);
            await PerformAsync(draws);
        }
    }
}
