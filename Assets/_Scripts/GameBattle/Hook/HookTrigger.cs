using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>触发管线：在时机点收集宿主状态产生的动作，只产出不执行（变更一律经动作系统）；
    /// 第一版时机为"宿主自己的回合结束"，行为按固定序执行——灼烧 → 再生，与收编前的阶段内顺序一致。</summary>
    public static class HookTrigger
    {
        #region 收集宿主"回合结束"时点产生的动作（宿主自己的回合边界）

        public static void CollectTurnEnd(Creature host_, List<Creature> self_, List<BattleAction> actions_)
        {
            BurnHook.Contribute(host_, self_, actions_);
            RegenHook.Contribute(host_, self_, actions_);
        }

        #endregion
    }
}
