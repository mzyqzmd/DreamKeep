using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>缓冲防护入口：只作用于"受伤"，不作用于"失去生命"——按 StS2 原版语义缓冲只挡伤害。
    /// 在 BA_Damage 的格挡之后、写血之前调用：把"未被格挡的剩余伤害"整体抵消并扣一层，每层抵一次。
    /// 完全被格挡时不消耗；攻击意图与卡面伤害预览不读本入口，数字保持正常显示。</summary>
    internal static class BufferHook
    {
        #region 抵消剩余伤害：返回被抵消掉的血量，调用方并入 Absorbed 让表现层按真实掉血渲染

        /// <summary>目标有缓冲层数且本次有未格挡伤害时，返回该伤害值并使剩余归零（由调用方用返回值写血）；
        /// 无目标、无状态、无剩余伤害时返回 0，不消耗层数。</summary>
        internal static int TryAbsorb(Creature target_, int hpLoss_, List<IBattleEvent> events_)
        {
            if (target_ == null || hpLoss_ <= 0 || target_.IsDead) return 0;

            IReadOnlyList<BattlePerk> perks = target_.Perks;
            for (int i = 0; i < perks.Count; i++)
            {
                if (perks[i] is not BP_Buffer { Layers: > 0 } buffer) continue;
                buffer.AddLayers(-1);
                if (buffer.Layers > 0)
                    events_.Add(new BE_PerkSettled(target_, buffer));
                else
                {
                    BattlePerk removed = target_.RemovePerk(buffer);
                    if (removed != null) events_.Add(new BE_PerkRemoved(target_, removed));
                }
                return hpLoss_;
            }

            return 0;
        }

        #endregion
    }
}
