using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>状态介入战斗的滑溜消费入口：宿主真实掉血后扣一层，幸存广播刷新层数，归零移除状态；
    /// 只合事件不进动作队列，照 HpDropHook / BA_RampageBreak 先例。</summary>
    internal static class SlipperyHook
    {
        #region 真实掉血后消费一层滑溜

        internal static void Consume(Creature target_, List<IBattleEvent> events_)
        {
            if (target_.IsDead) return;

            IReadOnlyList<BattlePerk> perks = target_.Perks;
            for (int i = 0; i < perks.Count; i++)
            {
                if (perks[i] is not BP_Slippery slippery) continue;
                slippery.AddLayers(-1);
                if (slippery.Layers > 0)
                    events_.Add(new BE_PerkSettled(target_, slippery));
                else
                {
                    BattlePerk removed = target_.RemovePerk(slippery);
                    if (removed != null) events_.Add(new BE_PerkRemoved(target_, removed));
                }
                break;
            }
        }

        #endregion
    }
}
