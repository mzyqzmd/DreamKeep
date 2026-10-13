using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>批量弃置动作：把指定牌堆中的一批卡一次移入弃牌堆，卡已离场则静默跳过，成功者合并发一条批量事件。</summary>
    public sealed class BA_DiscardBatch : BattleAction
    {
        private readonly IReadOnlyList<CardModel> _cardsToDiscard;
        private readonly CardPile _sourcePile;

        public BA_DiscardBatch(Creature source_, List<Creature> targets_, IReadOnlyList<CardModel> cards_, CardPile sourcePile_)
            : base(source_, targets_)
        {
            _cardsToDiscard = cards_;
            _sourcePile = sourcePile_;
        }

        public override List<IBattleEvent> Execute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();
            PlayerCombatState state = battleContext_.Player.CombatState;
            List<CardModel> discarded = new();

            foreach (CardModel card in _cardsToDiscard)
            {
                // 结算期间卡可能已被消耗或遗忘离场，移除失败即视为离场
                if (!_sourcePile.Remove(card)) continue;
                state.DiscardPile.Add(card);
                discarded.Add(card);
            }

            if (discarded.Count > 0) events.Add(new BE_CardsDiscarded(discarded));
            return events;
        }
    }
}
