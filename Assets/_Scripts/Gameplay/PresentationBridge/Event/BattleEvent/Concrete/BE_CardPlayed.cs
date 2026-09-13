using _DreamKeep.RuntimeData;

namespace _DreamKeep.Gameplay
{
    /// <summary>出牌：携带被打出的战斗卡，供表现层反查卡面类型与词条以决定播放哪个动作。</summary>
    public struct BE_CardPlayed : IBattleEvent
    {
        public CardModel Card { get; }

        public BE_CardPlayed(CardModel card_) => Card = card_;
    }
}
