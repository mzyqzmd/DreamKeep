namespace _DreamKeep.Gameplay
{
    /// <summary>抽牌：卡实例 Id 供命令引用，卡 Id 供表现层查卡面。</summary>
    public class CardDrawnEvent : IDomainEvent
    {
        public int CardInstanceId { get; }
        public string CardId { get; }

        public CardDrawnEvent(int cardInstanceId_, string cardId_)
        {
            CardInstanceId = cardInstanceId_;
            CardId = cardId_;
        }
    }
}
