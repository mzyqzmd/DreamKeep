using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>入打出堆动作：打出的第一步，把卡从手牌移入打出堆并写出出牌事件；
    /// 此后扣能量与词条结算都在打出堆已完成，卡在结算结束后按结果离场。</summary>
    public class BA_ToPlayPile : BattleAction
    {
        public CardModel Card { get; }

        public BA_ToPlayPile(Creature source_, List<Creature> targets_, CardModel card_) : base(source_, targets_) => Card = card_;

        protected override List<IBattleEvent> OnExecute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();
            PlayerCombatState state = battleContext_.Player.CombatState;
            if (!state.Hand.Remove(Card)) return events;

            state.PlayPile.Add(Card);
            events.Add(new BE_CardPlayed(Card));
            return events;
        }
    }
}
