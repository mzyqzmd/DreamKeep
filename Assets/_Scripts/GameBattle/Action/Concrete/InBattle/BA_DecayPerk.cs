using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>状态衰减动作：回合边界让目标身上该时机的状态层数递减，归零移除的逐个发移除事件，
    /// 幸存的逐个发递减事件供表现层刷新层数，玩家与敌人共用。</summary>
    public class BA_DecayPerk : BattleAction
    {
        private readonly EPerkDecayTiming _timing;

        public BA_DecayPerk(Creature source_, List<Creature> targets_, EPerkDecayTiming timing_) : base(source_, targets_) =>
            _timing = timing_;

        protected override List<IBattleEvent> OnExecute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();
            foreach (Creature target in Targets)
            {
                foreach (BattlePerk removed in target.DecayPerks(_timing))
                    events.Add(new BE_PerkRemoved(target, removed));

                // 幸存者：本时机衰减后仍挂载的实例，层数已递减，逐个广播供表现层对齐显示
                foreach (BattlePerk survived in target.Powers)
                    if (survived.DecayTiming == _timing) events.Add(new BE_PerkDecayed(target, survived));
            }

            return events;
        }
    }
}
