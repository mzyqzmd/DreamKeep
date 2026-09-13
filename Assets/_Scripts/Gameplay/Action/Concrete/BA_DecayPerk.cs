using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Gameplay
{
    /// <summary>状态衰减动作：回合结束让目标身上会衰减的状态层数递减，玩家与敌人共用。</summary>
    public class BA_DecayPerk : BattleAction
    {
        public BA_DecayPerk(Creature source_, List<Creature> targets_) : base(source_, targets_) { }

        protected override List<IBattleEvent> OnExecute(BattleContext battleContext_)
        {
            foreach (Creature target in Targets)
                target.DecayPerks();
            return new List<IBattleEvent>();
        }
    }
}
