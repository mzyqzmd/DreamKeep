using System;
using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>给予动作：为手牌中指定张数的卡动态追加一个词条（只增不删，移除由其他动作负责）。</summary>
    public sealed class BA_Grant : BattleAction
    {
        /// <summary>授予张数。</summary>
        public int Count { get; }

        /// <summary>词条原型，实际授予时逐张克隆。</summary>
        public CardAffix Granted { get; }

        public BA_Grant(Creature source_, List<Creature> targets_, int count_, CardAffix granted_)
            : base(source_, targets_)
        {
            Count = count_;
            Granted = granted_;
        }

        public override List<IBattleEvent> Execute(BattleContext battleContext_)
        {
            CardPile hand = battleContext_.Player.CombatState.Hand;
            int granted = Math.Min(Count, hand.Count);
            for (int i = 0; i < granted; i++)
                hand.Cards[i].AddAffix(Granted.Clone());
            return new List<IBattleEvent>();
        }
    }
}
