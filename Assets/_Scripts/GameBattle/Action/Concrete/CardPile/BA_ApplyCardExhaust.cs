using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>应用手牌选择结果：把选中的手牌逐张移入消耗堆，复用既有单张消耗事件链。</summary>
    public sealed class BA_ApplyCardExhaust : BattleAction
    {
        private readonly IReadOnlyList<CardModel> _cards;

        public BA_ApplyCardExhaust(Creature source_, List<Creature> targets_, IReadOnlyList<CardModel> cards_)
            : base(source_, targets_) => _cards = cards_;

        public override List<IBattleEvent> Execute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();
            PlayerCombatState state = battleContext_.Player.CombatState;
            foreach (CardModel card in _cards)
            {
                if (!state.Hand.Remove(card)) continue;
                state.ExhaustPile.Add(card);
                events.Add(new BE_CardExhausted(card));
            }

            return events;
        }
    }
}
