using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>棋子：每打出一张能力牌就抽牌。</summary>
    public sealed class RE_ChessPiece : RelicEffect
    {
        private readonly SO_ChessPiece _data;

        public RE_ChessPiece(SO_ChessPiece data_, BattleContext context_, ActionSystem actions_, BattleEventDispatcher dispatcher_,
            RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            _data = data_;
            Dispatcher.Register<BE_CardPlayed>(OnCardPlayed);
        }

        private async UniTask OnCardPlayed(BE_CardPlayed ev_)
        {
            if (ev_.Card.Type != ECardType.Ability) return;
            Creature self = Context.Player.Creature;
            List<Creature> target = new() { self };
            BattleAction[] draws = new BattleAction[_data.Cards];
            for (int i = 0; i < draws.Length; i++) draws[i] = new BA_DrawCard(self, target);
            await PerformAsync(draws);
        }
    }
}
