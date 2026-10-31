using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>怀表：本回合打出的牌不超过阈值时，下个玩家回合额外抽牌。
    /// 回合末落标记、下回合开窗（初始抽牌之后）兑现。</summary>
    public sealed class RE_PocketWatch : RelicEffect<SO_PocketWatch>
    {
        private int _played;
        private bool _bonusPending;

        public RE_PocketWatch(SO_PocketWatch data_, BattleContext context_, ActionSystem actions_,
            BattleEventDispatcher dispatcher_, RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            Dispatcher.Register<BE_CardPlayed>(OnCardPlayed);
            Dispatcher.Register<BE_PlayerTurnEnded>(OnPlayerTurnEnded);
            Dispatcher.Register<BE_PlayerTurnActiveStarted>(OnPlayerTurnActiveStarted);
        }

        private void OnCardPlayed(BE_CardPlayed ev_) => _played++;

        private void OnPlayerTurnEnded(BE_PlayerTurnEnded ev_)
        {
            _bonusPending = _played <= Data.MaxCards;
            _played = 0;
        }

        private async UniTask OnPlayerTurnActiveStarted(BE_PlayerTurnActiveStarted ev_)
        {
            if (!_bonusPending) return;
            _bonusPending = false;
            Creature self = Context.Player.Creature;
            List<Creature> target = PlayerTarget;
            BattleAction[] draws = new BattleAction[Data.Cards];
            for (int i = 0; i < draws.Length; i++) draws[i] = new BA_DrawCard(self, target);
            await PerformAsync(draws);
        }
    }
}
