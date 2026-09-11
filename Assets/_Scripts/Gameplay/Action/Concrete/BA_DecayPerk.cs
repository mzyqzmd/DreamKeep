using System.Collections.Generic;

namespace _DreamKeep.Gameplay
{
    /// <summary>状态衰减动作：回合结束让目标身上会衰减的状态层数递减，玩家与敌人共用。</summary>
    public class BA_DecayPerk : BattleAction
    {
        public BA_DecayPerk(Actor source_, List<Actor> targets_) : base(source_, targets_) { }

        protected override void OnExecute(BattleState battleState_, BattleEventBuffer events_)
        {
            foreach (Actor target in Targets)
                target.DecayPerks();
        }
    }
}
