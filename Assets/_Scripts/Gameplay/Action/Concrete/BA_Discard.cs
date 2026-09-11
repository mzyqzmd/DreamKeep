using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Gameplay
{
    /// <summary>弃置动作：把手牌中指定的卡移入弃牌堆，卡已离手则静默跳过。</summary>
    public class BA_Discard : BattleAction
    {
        /// <summary>延迟执行期间卡可能已被消耗或遗忘离场，移除失败即视为离场。</summary>
        public CardModel Card { get; }

        public BA_Discard(Actor source_, List<Actor> targets_, CardModel card_) : base(source_, targets_) => Card = card_;

        protected override void OnExecute(BattleState battleState_, BattleEventBuffer events_)
        {
            if (!battleState_.Hand.Remove(Card)) return;
            battleState_.DiscardPile.Add(Card);
            events_.Emit(new IBE_CardDiscarded(Card));
        }
    }
}
