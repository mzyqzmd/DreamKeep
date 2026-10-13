using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>遗忘动作：把这张卡从整局卡组永久移除，不进入任何牌堆；战斗结束时由结束阶段回写卡组。</summary>
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

            // 有源卡登记其局内卡引用供战斗结束回写卡组，战斗内临时卡查不到映射则跳过
            if (battleContext_.TryGetRunCard(_cardToErase, out CardModel runCard))
                battleContext_.RegisterErased(runCard);

            events.Add(new BE_CardErased(_cardToErase));
            return events;
        }
    }
}
