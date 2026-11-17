using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>状态介入战斗的消耗入口：一张牌进入消耗堆后询问一次宿主状态，黑暗之拥据此抽牌、无惧疼痛据此得格挡；
    /// 只产出不执行——嵌套动作直接执行并合事件，不进动作队列（照 SlipperyHook 先例）。</summary>
    internal static class ExhaustHook
    {
        #region 消耗后触发：黑暗之拥抽牌、无惧疼痛得格挡，两者可同时挂载

        internal static void Check(BattleContext context_, Creature host_, List<IBattleEvent> events_)
        {
            if (host_ == null || host_.IsDead) return;

            List<Creature> self = new List<Creature> { host_ };
            BP_DarkEmbrace darkEmbrace = BattlePerkQuery.Find<BP_DarkEmbrace>(host_);
            if (darkEmbrace != null)
                for (int i = 0; i < darkEmbrace.Layers; i++)
                    events_.AddRange(new BA_DrawCard(host_, self).Execute(context_));

            BP_FeelNoPain feelNoPain = BattlePerkQuery.Find<BP_FeelNoPain>(host_);
            if (feelNoPain != null && feelNoPain.Layers > 0)
                events_.AddRange(new BA_Block(host_, self, feelNoPain.Layers).Execute(context_));
        }

        #endregion
    }
}
