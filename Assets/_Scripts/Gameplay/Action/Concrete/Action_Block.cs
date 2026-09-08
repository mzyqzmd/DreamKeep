using System.Collections.Generic;

namespace _DreamKeep.Gameplay
{
    /// <summary>格挡动作：为目标累加格挡，格挡优先吸收后续伤害。</summary>
    public class Action_Block : GameAction
    {
        /// <summary>可被 Pre 钩子修改的格挡值。</summary>
        public int Amount { get; set; }

        public Action_Block(Actor source_, List<Actor> targets_, int amount_) : base(source_, targets_) => Amount = amount_;

        protected override void OnExecute(BattleState battleState_, DomainEventBuffer events_)
        {
            foreach (Actor target in Targets)
            {
                if (target.IsDead) continue;
                target.Block += Amount;
                events_.Emit(new BlockGainedEvent(target, Amount));
            }
        }
    }
}
