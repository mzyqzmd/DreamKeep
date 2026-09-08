using System.Collections.Generic;

namespace _DreamKeep.Gameplay
{
    /// <summary>清空格挡动作：敌人回合开始清除上回合残留格挡，区别于只做累加的格挡动作。</summary>
    public class Action_ClearBlock : GameAction
    {
        public Action_ClearBlock(Actor source_, List<Actor> targets_) : base(source_, targets_)
        {
        }

        protected override void OnExecute(BattleState battleState_, DomainEventBuffer events_)
        {
            foreach (Actor target in Targets)
                target.Block = 0;
        }
    }
}
