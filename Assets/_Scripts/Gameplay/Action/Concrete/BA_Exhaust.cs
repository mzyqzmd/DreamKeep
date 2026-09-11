using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Gameplay
{
    /// <summary>消耗动作：把自身这张卡从手牌移入消耗区，本场战斗不再被抽到。</summary>
    public class BA_Exhaust : BattleAction
    {
        /// <summary>延迟执行期间卡可能已离场，移除失败即视为离场。</summary>
        public CardModel Card { get; }

        public BA_Exhaust(Actor source_, List<Actor> targets_, CardModel card_) : base(source_, targets_) => Card = card_;

        protected override void OnExecute(BattleState battleState_, BattleEventBuffer events_)
        {
            if (!battleState_.Hand.Remove(Card)) return;
            battleState_.ExhaustPile.Add(Card);
            events_.Emit(new IBE_CardExhausted(Card));
        }
    }
}
