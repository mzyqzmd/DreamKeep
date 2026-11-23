using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>升级整手：把宿主当前手牌中所有可升级牌一次升级，逐张发升级事件；战斗副本升级，不写回局内卡组。</summary>
    public sealed class BA_UpgradeHand : BattleAction
    {
        public BA_UpgradeHand(Creature source_, List<Creature> targets_) : base(source_, targets_) { }

        public override List<IBattleEvent> Execute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();
            IReadOnlyList<CardModel> cards = battleContext_.Player.CombatState.Hand.Cards;
            for (int i = 0; i < cards.Count; i++)
            {
                if (!cards[i].CanUpgrade) continue;
                cards[i].Upgrade();
                events.Add(new BE_CardUpgraded(cards[i]));
            }
            return events;
        }
    }
}
