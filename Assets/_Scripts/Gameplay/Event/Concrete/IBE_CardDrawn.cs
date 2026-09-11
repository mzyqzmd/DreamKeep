using _DreamKeep.RuntimeData;

namespace _DreamKeep.Gameplay
{
    /// <summary>抽牌：直接携带卡本身，卡面与身份一并由卡提供。</summary>
    public struct IBE_CardDrawn : IBattleEvent
    {
        public CardModel Card { get; }

        public IBE_CardDrawn(CardModel card_) => Card = card_;
    }
}
