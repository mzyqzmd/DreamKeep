using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Gameplay
{
    /// <summary>给予动作：为手牌中指定张数的卡动态追加一个词条，支持运行时增删。</summary>
    public class Action_Grant : GameAction
    {
        public int Count { get; set; }
        public CardAffix Granted { get; set; }

        public Action_Grant(Actor source_, List<Actor> targets_, int count_, CardAffix granted_)
            : base(source_, targets_)
        {
            Count = count_;
            Granted = granted_;
        }

        protected override void OnExecute(BattleState battleState_, DomainEventBuffer events_)
        {
            CardPile hand = battleState_.Hand;
            int granted = Count < hand.Count ? Count : hand.Count;
            for (int i = 0; i < granted; i++)
                hand.Cards[i].AddAffix(Granted.Clone());
        }
    }
}
