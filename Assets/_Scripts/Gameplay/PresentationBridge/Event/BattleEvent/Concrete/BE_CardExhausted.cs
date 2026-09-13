using _DreamKeep.RuntimeData;

namespace _DreamKeep.Gameplay
{
    /// <summary>消耗：卡进入消耗区，本场战斗不再被抽到（结束重洗回牌组）。</summary>
    public struct BE_CardExhausted : IBattleEvent
    {
        public CardModel Card { get; }

        public BE_CardExhausted(CardModel card_) => Card = card_;
    }
}
