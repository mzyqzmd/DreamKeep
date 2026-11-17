using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>状态介入战斗的施加入口：一次状态挂载完成后询问一次施加方状态，凶恶据此在给予易伤时抽牌；
    /// 只产出不执行，事件并入调用方收集表。</summary>
    internal static class PerkApplyHook
    {
        #region 施加状态后触发：凶恶在给予易伤时抽牌

        internal static void Check(BattleContext context_, Creature source_, Creature target_, BattlePerk applied_,
            List<IBattleEvent> events_)
        {
            if (source_ == null || source_.IsDead || applied_ is not BP_Vulnerable) return;

            BP_Vicious vicious = BattlePerkQuery.Find<BP_Vicious>(source_);
            if (vicious == null) return;

            List<Creature> self = new List<Creature> { source_ };
            for (int i = 0; i < vicious.Layers; i++)
                events_.AddRange(new BA_DrawCard(source_, self).Execute(context_));
        }

        #endregion
    }
}
