namespace _DreamKeep.Gameplay
{
    /// <summary>消耗：卡进入消耗区，本场战斗不再被抽到（结束重洗回牌组）。</summary>
    public class CardExhaustedEvent : IDomainEvent
    {
        public int CardInstanceId { get; }

        public CardExhaustedEvent(int cardInstanceId_) => CardInstanceId = cardInstanceId_;
    }
}
