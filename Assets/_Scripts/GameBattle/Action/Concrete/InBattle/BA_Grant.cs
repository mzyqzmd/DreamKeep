using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>给予动作：为手牌中指定张数的卡动态追加一个词条，支持运行时增删。</summary>
    public class BA_Grant : BattleAction
    {
        /// <summary>可被 Pre 钩子修改的授予张数。</summary>
        public int Count { get; set; }
        /// <summary>可被 Pre 钩子替换的词条原型，实际授予时逐张克隆。</summary>
        public CardAffix Granted { get; set; }

        public BA_Grant(Creature source_, List<Creature> targets_, int count_, CardAffix granted_)
            : base(source_, targets_)
        {
            Count = count_;
            Granted = granted_;
        }

        protected override List<IBattleEvent> OnExecute(BattleContext battleContext_)
        {
            CardPile hand = battleContext_.Player.CombatState.Hand;
            int granted = Count < hand.Count ? Count : hand.Count;
            for (int i = 0; i < granted; i++)
                hand.Cards[i].AddAffix(Granted.Clone());
            return new List<IBattleEvent>();
        }
    }
}
