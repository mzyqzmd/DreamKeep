using System.Collections.Generic;

namespace _DreamKeep.Gameplay
{
    /// <summary>状态衰减动作：回合结束让目标身上会衰减的状态层数递减，玩家与敌人共用。</summary>
    public class Action_DecayStatus : GameAction
    {
        public Action_DecayStatus(Actor source_, List<Actor> targets_) : base(source_, targets_)
        {
        }

        protected override void OnExecute(BattleState battleState_, DomainEventBuffer events_)
        {
            foreach (Actor target in Targets)
                target.DecayPerks();
        }
    }
}
