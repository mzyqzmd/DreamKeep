using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>金纸：每消耗 N 张牌抽牌，计数全战斗累计、不按回合重置。</summary>
    public sealed class RE_GoldPaper : RelicEffect
    {
        private readonly SO_GoldPaper _data;
        private int _count;

        public RE_GoldPaper(SO_GoldPaper data_, BattleContext context_, ActionSystem actions_, BattleEventDispatcher dispatcher_,
            RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            _data = data_;
            Dispatcher.Register<BE_CardExhausted>(OnCardExhausted);
        }

        private async UniTask OnCardExhausted(BE_CardExhausted ev_)
        {
            if (++_count < _data.Count) return;
            _count = 0;
            Creature self = Context.Player.Creature;
            List<Creature> target = new() { self };
            BattleAction[] draws = new BattleAction[_data.Cards];
            for (int i = 0; i < draws.Length; i++) draws[i] = new BA_DrawCard(self, target);
            await PerformAsync(draws);
        }
    }
}
