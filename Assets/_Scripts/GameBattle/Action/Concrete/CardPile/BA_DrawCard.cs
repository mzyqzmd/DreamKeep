using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>抽牌动作：从抽牌堆抽一张入手牌，手牌已达上限时本张不入手牌、直接进弃牌堆（抽牌堆照常被抽走一张）；抽牌堆见底时先把弃牌堆整摞洗回，并广播洗回事件（视图据此播搬运演出，播完才轮到抽牌入场）。抽多张请入队多个动作，使每张独立结算、各自判定手牌上限。</summary>
    public sealed class BA_DrawCard : BattleAction
    {
        public BA_DrawCard(Creature source_, List<Creature> targets_) : base(source_, targets_)
        {
        }

        public override List<IBattleEvent> Execute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();
            PlayerCombatState state = battleContext_.Player.CombatState;

            if (state.DrawPile.Count == 0)
            {
                if (state.DiscardPile.Count == 0)
                {
                    events.Add(new BE_NoCardCanDraw());
                    return events;
                }

                // 洗回先于抽牌广播：事件按列表顺序逐条 await 演出，洗回搬运播完才轮到本次抽牌
                int reshuffled = state.DiscardPile.Count;
                state.DiscardPile.MoveAllTo(state.DrawPile);
                state.DrawPile.Shuffle(battleContext_.Rng);
                events.Add(new BE_DiscardReshuffled(reshuffled));
            }

            CardModel drawn = state.DrawPile.DrawTop();

            // 手牌上限：抽牌照常走完（洗回与抽牌堆消耗都已发生），装不下的这张不入手牌、直接进弃牌堆
            if (state.Hand.Count >= PlayerCombatState.MaxHandSize)
            {
                state.DiscardPile.Add(drawn);
                events.Add(new BE_CardDiscarded(drawn));
                events.Add(new BE_HandOverflowed()); // 溢出的独立事实：弃牌计数仍由上面那条负责
                return events;
            }

            state.Hand.Add(drawn);
            events.Add(new BE_CardDrawn(drawn));
            return events;
        }
    }
}
