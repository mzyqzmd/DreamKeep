using System.Collections.Generic;
using _DreamKeep.Data;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Gameplay
{
    /// <summary>施加状态动作：为目标挂载状态层数，同类型累加。</summary>
    public class BA_ApplyPerk : BattleAction
    {
        /// <summary>可被 Pre 钩子替换的状态实例，层数由实例携带。</summary>
        public BattlePerk Perk { get; set; }

        public BA_ApplyPerk(Creature source_, List<Creature> targets_, BattlePerk perk_) : base(source_, targets_) =>
            Perk = perk_;

        protected override List<IBattleEvent> OnExecute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();
            foreach (Creature target in Targets)
            {
                if (target.IsDead) continue;
                int layers = target.ApplyPerk(Perk);
                events.Add(new BE_PerkApplied(target, Perk, layers));
            }

            return events;
        }
    }
}
