using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>抽牌动作：从抽牌堆抽一张入手牌，手牌已达上限时本张落空（不入手牌、不洗回、抽牌堆不动），抽牌堆见底时先把弃牌堆整摞洗回，并广播洗回事件（视图据此播搬运演出，播完才轮到抽牌入场）。抽多张请入队多个动作，使每张独立结算、各自判定手牌上限。</summary>
    public sealed class BA_DrawCard : BattleAction
    {
        public BA_DrawCard(Creature source_, List<Creature> targets_) : base(source_, targets_)
        {
        }

        public override List<IBattleEvent> Execute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();
            PlayerCombatState state = battleContext_.Player.CombatState;

            // 手牌上限硬闸：满即整张落空，优先级高于洗回——抽不到牌就不该白搬一摞弃牌回去
            if (state.Hand.Count >= PlayerCombatState.MaxHandSize) return events;

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
            state.Hand.Add(drawn);
            events.Add(new BE_CardDrawn(drawn));
            return events;
        }
    }
}
