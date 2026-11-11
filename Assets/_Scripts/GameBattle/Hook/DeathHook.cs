using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>状态介入战斗的死亡入口：生物死亡时摘除全场由它施加的「缩小」，
    /// 事件并入调用方的收集表（照 HpDropHook 先例），让表现层统一还原槽位与视觉缩放。</summary>
    internal static class DeathHook
    {
        #region 死亡清理：按施加者精确摘除缩小状态

        /// <summary>遍历玩家与全部敌人，移除 Source 为死者的缩小实例并广播移除事件。</summary>
        internal static void Cleanup(BattleContext context_, Creature dead_, List<IBattleEvent> events_)
        {
            CleanupHost(context_.Player.Creature, dead_, events_);
            IReadOnlyList<Enemy> enemies = context_.Enemies;
            for (int i = 0; i < enemies.Count; i++)
                CleanupHost(enemies[i].Creature, dead_, events_);
        }

        // 同类型状态唯一，命中即摘除并停止遍历（RemovePerk 会改动宿主的状态表）
        private static void CleanupHost(Creature host_, Creature dead_, List<IBattleEvent> events_)
        {
            IReadOnlyList<BattlePerk> perks = host_.Perks;
            for (int i = 0; i < perks.Count; i++)
            {
                if (perks[i] is not BP_Shrink shrink || shrink.Source != dead_) continue;
                BattlePerk removed = host_.RemovePerk(shrink);
                if (removed != null) events_.Add(new BE_PerkRemoved(host_, removed));
                return;
            }
        }

        #endregion
    }
}
