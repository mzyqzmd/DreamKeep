using _DreamKeep.RuntimeData;

namespace _DreamKeep.Gameplay
{
    /// <summary>抽牌：直接携带卡本身，卡面与身份一并由卡提供。</summary>
    public struct BE_CardDrawn : IBattleEvent
    {
        public CardModel Card { get; }

        public BE_CardDrawn(CardModel card_) => Card = card_;
    }
}
