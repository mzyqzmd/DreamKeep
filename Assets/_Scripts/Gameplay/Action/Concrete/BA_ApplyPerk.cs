using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Gameplay
{
    /// <summary>施加状态动作：为目标挂载状态层数，同类型累加。</summary>
    public class BA_ApplyPerk : BattleAction
    {
        /// <summary>可被 Pre 钩子替换的状态原型，层数由原型携带。</summary>
        public BattlePerk Perk { get; set; }

        public BA_ApplyPerk(Actor source_, List<Actor> targets_, BattlePerk perk_) : base(source_, targets_) => Perk = perk_;

        protected override void OnExecute(BattleState battleState_, BattleEventBuffer events_)
        {
            foreach (Actor target in Targets)
            {
                if (target.IsDead) continue;
                int layers = target.ApplyPerk(Perk);
                events_.Emit(new IBE_PerkApplied(target, Perk.Name, layers));
            }
        }
    }
}
