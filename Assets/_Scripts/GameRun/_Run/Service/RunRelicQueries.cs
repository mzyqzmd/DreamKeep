using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>跨段遗物能力查询：把非战斗遗物的持有判定收口在一处，段循环只问能力数值、不判具体类型；
    /// 一局一份由整局容器注册，无状态不缓存，新增同类遗物只改这里。</summary>
    public sealed class RunRelicQueries
    {
        private readonly RunState _run;

        public RunRelicQueries(RunState run_) => _run = run_;

        #region 能力查询：遍历整局遗物栏汇总数值，重复持有按件累加或取最强

        /// <summary>休息选项的额外回血：所有皇家枕头配置的合计。</summary>
        public int RestHealBonus()
        {
            int total = 0;
            IReadOnlyList<SO_Relic> relics = _run.Player.Relics;
            for (int i = 0; i < relics.Count; i++)
                if (relics[i] is SO_RegalPillow pillow) total += pillow.Heal;
            return total;
        }

        /// <summary>进店回血：所有餐券配置的合计。</summary>
        public int ShopEntryHeal()
        {
            int total = 0;
            IReadOnlyList<SO_Relic> relics = _run.Player.Relics;
            for (int i = 0; i < relics.Count; i++)
                if (relics[i] is SO_MealTicket ticket) total += ticket.Heal;
            return total;
        }

        /// <summary>商店售价系数：取会员卡中最强折扣，未持有时为 1。</summary>
        public float ShopPriceMultiplier()
        {
            float multiplier = 1f;
            IReadOnlyList<SO_Relic> relics = _run.Player.Relics;
            for (int i = 0; i < relics.Count; i++)
            {
                if (relics[i] is not SO_MembershipCard card) continue;
                float candidate = 1f - card.Discount;
                if (candidate < multiplier) multiplier = candidate;
            }
            return multiplier;
        }

        /// <summary>标记下一场战斗首回合领额外能量：以每件古茶具遗物为 key 写计数 1（幂等）。</summary>
        public void MarkNextBattleEnergy()
        {
            IReadOnlyList<SO_Relic> relics = _run.Player.Relics;
            for (int i = 0; i < relics.Count; i++)
                if (relics[i] is SO_VenerableTeaSet) _run.Player.RestoreRelicCounter(relics[i], 1);
        }

        #endregion
    }
}
