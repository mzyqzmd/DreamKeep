using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>状态结算动作：让目标身上该时机的状态结算一次 —— 先执行触发效果，再让层数递减一层；
    /// 归零移除的逐个发移除事件，幸存的逐个发结算事件供表现层刷新层数，玩家与敌人共用。</summary>
    public sealed class BA_SettlePerk : BattleAction
    {
        private readonly EPerkSettleTiming _timing;

        public BA_SettlePerk(Creature source_, List<Creature> targets_, EPerkSettleTiming timing_) : base(source_, targets_) =>
            _timing = timing_;

        public override List<IBattleEvent> Execute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();
            foreach (Creature target in Targets)
            {
                // 触发读的是递减前的层数，必须先于 SettlePerks；嵌套动作只合事件，不进动作队列
                List<BattleAction> triggered = new();
                SettleHook.Settle(target, _timing, triggered);
                for (int i = 0; i < triggered.Count; i++)
                    events.AddRange(triggered[i].Execute(battleContext_));

                foreach (BattlePerk removed in target.SettlePerks(_timing))
                    events.Add(new BE_PerkRemoved(target, removed));

                // 幸存者：本时机结算后仍挂载的实例，层数已递减，逐个广播供表现层对齐显示
                foreach (BattlePerk survived in target.Perks)
                    if (survived.SettleTiming == _timing) events.Add(new BE_PerkSettled(target, survived));
            }

            return events;
        }
    }
}
