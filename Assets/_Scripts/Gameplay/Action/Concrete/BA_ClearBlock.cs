using System.Collections.Generic;

namespace _DreamKeep.Gameplay
{
    /// <summary>清空格挡动作：回合开始清除残留格挡，区别于只做累加的格挡动作。</summary>
    public class BA_ClearBlock : BattleAction
    {
        public BA_ClearBlock(Actor source_, List<Actor> targets_) : base(source_, targets_) { }

        protected override void OnExecute(BattleState battleState_, BattleEventBuffer events_)
        {
            foreach (Actor target in Targets)
            {
                if (target.Block <= 0) continue;
                target.Block = 0;
                events_.Emit(new IBE_BlockCleared(target));
            }
        }
    }
}
