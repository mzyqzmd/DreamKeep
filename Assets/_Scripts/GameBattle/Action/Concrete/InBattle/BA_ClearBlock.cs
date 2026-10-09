using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>清空格挡动作：回合开始清除残留格挡，区别于只做累加的格挡动作。</summary>
    public sealed class BA_ClearBlock : BattleAction
    {
        public BA_ClearBlock(Creature source_, List<Creature> targets_) : base(source_, targets_) { }

        protected override List<IBattleEvent> OnExecute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();
            foreach (Creature target in Targets)
            {
                if (target.Block <= 0) continue;
                target.Block = 0;
                events.Add(new BE_BlockCleared(target));
            }
            return events;
        }
    }
}
