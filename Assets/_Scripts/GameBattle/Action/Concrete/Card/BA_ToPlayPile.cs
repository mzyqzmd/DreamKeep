using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>入打出堆动作：打出的第一步，把卡从手牌移入打出堆并写出出牌事件；
    /// 此后扣能量与词条结算都在打出堆已完成，卡在结算结束后按结果离场。</summary>
    public sealed class BA_ToPlayPile : BattleAction
    {
        public CardModel Card { get; }

        public BA_ToPlayPile(Creature source_, List<Creature> targets_, CardModel card_) : base(source_, targets_) => Card = card_;

        public override List<IBattleEvent> Execute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();
            PlayerCombatState state = battleContext_.Player.CombatState;
            if (!state.Hand.Remove(Card)) return events;

            state.PlayPile.Add(Card);
            state.CardsPlayedThisTurn++; // 本回合出牌计数：昏眩限牌的唯一依据
            events.Add(new BE_CardPlayed(Card));
            return events;
        }
    }
}
