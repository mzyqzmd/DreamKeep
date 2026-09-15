using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>遗忘：卡从整局 run 永久移除，表现层可据此播抹除反馈。</summary>
    public struct BE_CardErased : IBattleEvent
    {
        public CardModel Card { get; }

        public BE_CardErased(CardModel card_) => Card = card_;
    }
}
