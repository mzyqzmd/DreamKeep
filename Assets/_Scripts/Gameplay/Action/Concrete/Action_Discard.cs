using _DreamKeep.RuntimeData;
using System.Collections.Generic;

namespace _DreamKeep.Gameplay
{
    /// <summary>弃置动作：把手牌中指定实例的卡移入弃牌堆，卡已离手则静默跳过。</summary>
    public class Action_Discard : GameAction
    {
        /// <summary>延迟执行期间卡可能已被消耗或遗忘离场，故记实例 Id 而非卡引用。</summary>
        public int CardInstanceId { get; }

        public Action_Discard(Actor source_, List<Actor> targets_, int cardInstanceId_)
            : base(source_, targets_) => CardInstanceId = cardInstanceId_;

        protected override void OnExecute(BattleState battleState_, DomainEventBuffer events_)
        {
            InBattleCard card = battleState_.Hand.Find(CardInstanceId);
            if (card == null) return;
            battleState_.Hand.Remove(card);
            battleState_.DiscardPile.Add(card);
            events_.Emit(new CardDiscardedEvent(card.InstanceId));
        }
    }
}
