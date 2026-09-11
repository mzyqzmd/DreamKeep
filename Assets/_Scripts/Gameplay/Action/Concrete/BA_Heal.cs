using System.Collections.Generic;

namespace _DreamKeep.Gameplay
{
    /// <summary>治疗动作：为目标恢复血量，用于再生等回复类效果。</summary>
    public class BA_Heal : BattleAction
    {
        /// <summary>可被 Pre 钩子修改的回复量。</summary>
        public int Amount { get; set; }

        public BA_Heal(Actor source_, List<Actor> targets_, int amount_) : base(source_, targets_) => Amount = amount_;

        protected override void OnExecute(BattleState battleState_, BattleEventBuffer events_)
        {
            foreach (Actor target in Targets)
            {
                target.Health += Amount;
                events_.Emit(new IBE_HealthRestored(target, Amount));
            }
        }
    }
}
