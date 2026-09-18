using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>伤害动作：先扣格挡再扣血，直接按基础值结算（伤害修正/状态效果为后续扩展点，当前最小循环不含）。</summary>
    public class BA_Damage : BattleAction
    {
        /// <summary>基础伤害值。</summary>
        public int BaseAmount { get; }

        public BA_Damage(Creature source_, List<Creature> targets_, int amount_) : base(source_, targets_)
        {
            BaseAmount = amount_;
        }

        protected override List<IBattleEvent> OnExecute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();
            foreach (Creature target in Targets)
            {
                if (target.IsDead) continue;

                int final = BaseAmount;
                int absorbed = target.Block < final ? target.Block : final;
                target.Block -= absorbed;
                target.CurrentHp -= final - absorbed;

                //TODO:当前无消费者，到时候做了敌人才有用
                events.Add(new BE_DamageDealt(Source, target, final, absorbed));
                if (target.IsDead)
                    events.Add(new BE_ActorDied(target));
            }
            return events;
        }
    }
}
