using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>遗忘动作：结算当刻就把这张卡从整局卡组永久移除，不进入任何牌堆——顶栏牌库面板与后续各段读到的都是最终态。</summary>
    public sealed class BA_Erase : BattleAction
    {
        /// <summary>结算期间卡可能已离场，移除失败即视为离场。</summary>
        private readonly CardModel _cardToErase;

        public BA_Erase(Creature source_, List<Creature> targets_, CardModel cardToErase_) : base(source_, targets_) =>
            _cardToErase = cardToErase_;

        public override List<IBattleEvent> Execute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();
            if (!battleContext_.Player.CombatState.PlayPile.Remove(_cardToErase)) return events;

            // 有源卡即时写局内卡组；战斗内临时卡查不到映射则跳过
            if (battleContext_.TryGetRunCard(_cardToErase, out CardModel runCard))
                battleContext_.Player.Deck.Remove(runCard);

            events.Add(new BE_CardErased(_cardToErase));
            return events;
        }
    }
}
