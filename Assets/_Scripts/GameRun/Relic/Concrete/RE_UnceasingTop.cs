using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>不休陀螺：玩家回合内手牌打空时抽牌。
    /// 用"回合活跃"标记把回合末弃牌造成的空手挡在外面，只有出牌或效果弃牌才触发。</summary>
    public sealed class RE_UnceasingTop : RelicEffect<SO_UnceasingTop>
    {
        private bool _active;

        public RE_UnceasingTop(SO_UnceasingTop data_, BattleContext context_, ActionSystem actions_,
            BattleEventDispatcher dispatcher_, RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            Dispatcher.Register<BE_PlayerTurnActiveStarted>(OnPlayerTurnActiveStarted);
            Dispatcher.Register<BE_PlayerTurnEnded>(OnPlayerTurnEnded);
            Dispatcher.Register<BE_CardPlayed>(OnCardPlayed);
            Dispatcher.Register<BE_CardDiscarded>(OnCardDiscarded);
        }

        private void OnPlayerTurnActiveStarted(BE_PlayerTurnActiveStarted ev_) => _active = true;

        private void OnPlayerTurnEnded(BE_PlayerTurnEnded ev_) => _active = false;

        private async UniTask OnCardPlayed(BE_CardPlayed ev_) => await TryDrawAsync();

        private async UniTask OnCardDiscarded(BE_CardDiscarded ev_) => await TryDrawAsync();

        private async UniTask TryDrawAsync()
        {
            if (!_active || Context.Player.CombatState.Hand.Count != 0) return;
            Creature self = Context.Player.Creature;
            List<Creature> target = PlayerTarget;
            BattleAction[] draws = new BattleAction[Data.Cards];
            for (int i = 0; i < draws.Length; i++) draws[i] = new BA_DrawCard(self, target);
            await PerformAsync(draws);
        }
    }
}
