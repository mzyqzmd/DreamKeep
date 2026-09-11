using _DreamKeep.RuntimeData;

namespace _DreamKeep.Gameplay
{
    /// <summary>出牌：携带被打出的战斗卡，供表现层反查卡面类型与词条以决定播放哪个动作。</summary>
    public struct IBE_CardPlayed : IBattleEvent
    {
        public CardModel Card { get; }

        public IBE_CardPlayed(CardModel card_) => Card = card_;
    }
}
