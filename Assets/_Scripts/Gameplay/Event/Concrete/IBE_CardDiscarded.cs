using _DreamKeep.RuntimeData;

namespace _DreamKeep.Gameplay
{
    /// <summary>弃牌：回合结束或效果导致的弃置。</summary>
    public struct IBE_CardDiscarded : IBattleEvent
    {
        public CardModel Card { get; }

        public IBE_CardDiscarded(CardModel card_) => Card = card_;
    }
}
