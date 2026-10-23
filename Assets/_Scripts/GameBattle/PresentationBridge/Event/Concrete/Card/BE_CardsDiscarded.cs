using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>批量弃牌：一波（如回合末整手）弃置，列表顺序即弃牌堆顺序。</summary>
    public struct BE_CardsDiscarded : IBattleEvent
    {
        public IReadOnlyList<CardModel> Cards { get; }

        public BE_CardsDiscarded(IReadOnlyList<CardModel> cards_) => Cards = cards_;
    }
}
