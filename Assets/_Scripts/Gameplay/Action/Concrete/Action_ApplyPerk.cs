using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Gameplay
{
    /// <summary>施加状态动作：为目标挂载状态层数，同类型累加。</summary>
    public class Action_ApplyPerk : GameAction
    {
        /// <summary>可被 Pre 钩子替换的状态原型，层数由原型携带。</summary>
        public InBattlePerk Perk { get; set; }

        public Action_ApplyPerk(Actor source_, List<Actor> targets_, InBattlePerk perk_)
            : base(source_, targets_) => Perk = perk_;

        protected override void OnExecute(BattleState battleState_, DomainEventBuffer events_)
        {
            foreach (Actor target in Targets)
            {
                if (target.IsDead) continue;
                int layers = target.ApplyPerk(Perk);
                events_.Emit(new StatusAppliedEvent(target, Perk.Name, layers));
            }
        }
    }
}
