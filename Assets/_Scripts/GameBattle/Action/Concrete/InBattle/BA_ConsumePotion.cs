using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>消耗药水动作：从槽位取走那瓶药水，取不到即视为已离槽；成功则广播离槽事件并把落点一并带出
    /// （Targets 即玩法层解析好的落点，表现层据此演投掷飞行）。丢弃不经本动作，走 BattleLoop.DiscardPotionNow。</summary>
    public sealed class BA_ConsumePotion : BattleAction
    {
        public int SlotIndex { get; }

        public BA_ConsumePotion(Creature source_, List<Creature> targets_, int slotIndex_)
            : base(source_, targets_) => SlotIndex = slotIndex_;

        public override List<IBattleEvent> Execute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();
            if (!battleContext_.Player.TryTakePotion(SlotIndex, out SO_Potion potion)) return events;

            events.Add(new BE_PotionConsumed(SlotIndex, potion, Targets));
            return events;
        }
    }
}
