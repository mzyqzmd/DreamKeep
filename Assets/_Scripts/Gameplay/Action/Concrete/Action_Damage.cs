using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Gameplay
{
    /// <summary>伤害动作：先扣格挡再扣血，力量/虚弱作用于来源，易伤作用于目标；置 IgnoresModifiers 可跳过全部修正。</summary>
    public class Action_Damage : GameAction
    {
        /// <summary>可被 Pre 钩子修改的最终伤害基数。</summary>
        public int Amount { get; set; }
        /// <summary>可被 Pre 钩子切换：为真时跳过力量/虚弱/易伤修正，用于灼烧等无来源伤害。</summary>
        public bool IgnoresModifiers { get; set; }

        public Action_Damage(Actor source_, List<Actor> targets_, int amount_) : base(source_, targets_) => Amount = amount_;

        protected override void OnExecute(BattleState battleState_, DomainEventBuffer events_)
        {
            foreach (Actor target in Targets)
            {
                if (target.IsDead) continue;

                int final = Amount;
                if (!IgnoresModifiers)
                    final += Source.GetPerkLayers<IBP_Strength>()
                             - Source.GetPerkLayers<IBP_Weak>()
                             + target.GetPerkLayers<IBP_Vulnerable>();
                if (final < 0) final = 0;

                int absorbed = target.Block < final ? target.Block : final;
                target.Block -= absorbed;
                target.Health -= final - absorbed;

                events_.Emit(new DamageDealtEvent(Source, target, final, absorbed));
                if (target.IsDead)
                    events_.Emit(new ActorDiedEvent(target));
            }
        }
    }
}
