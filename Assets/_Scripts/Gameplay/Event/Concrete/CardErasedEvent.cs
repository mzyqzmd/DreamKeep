namespace _DreamKeep.Gameplay
{
    /// <summary>遗忘：卡从整局 run 永久移除，表现层可据此播抹除反馈。</summary>
    public class CardErasedEvent : IDomainEvent
    {
        public int CardInstanceId { get; }

        public CardErasedEvent(int cardInstanceId_) => CardInstanceId = cardInstanceId_;
    }
}
