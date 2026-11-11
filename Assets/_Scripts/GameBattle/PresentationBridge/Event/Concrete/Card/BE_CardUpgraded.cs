using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>本场战斗内一张手牌被升级：只改战斗副本，不写回局内卡组。</summary>
    public struct BE_CardUpgraded : IBattleEvent
    {
        public CardModel Card { get; }

        public BE_CardUpgraded(CardModel card_) => Card = card_;
    }
}
