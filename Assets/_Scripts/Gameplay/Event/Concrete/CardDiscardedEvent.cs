namespace _DreamKeep.Gameplay
{
    /// <summary>弃牌：回合结束或效果导致的弃置。</summary>
    public class CardDiscardedEvent : IDomainEvent
    {
        public int CardInstanceId { get; }

        public CardDiscardedEvent(int cardInstanceId_) => CardInstanceId = cardInstanceId_;
    }
}
