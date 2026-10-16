using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>触发管线：在时机点收集宿主状态产生的动作，只产出不执行（变更一律经动作系统）。
    /// 第一版时机为"宿主自己的回合结束"，按宿主的状态列表顺序（即施加顺序）逐条产出——
    /// 中毒与再生的先后由挂载顺序决定，不再由代码写死。</summary>
    public static class HookTrigger
    {
        #region 收集宿主"回合结束"时点产生的动作（宿主自己的回合边界）

        public static void CollectTurnEnd(Creature host_, List<Creature> self_, List<BattleAction> actions_)
        {
            IReadOnlyList<BattlePerk> powers = host_.Powers;
            for (int i = 0; i < powers.Count; i++)
            {
                switch (powers[i])
                {
                    // 中毒：纯状态伤害，不吃数值修正，可被格挡吸收
                    case BP_Poison poison:
                        if (poison.Layers > 0) actions_.Add(new BA_Damage(null, self_, poison.Layers, false));
                        break;
                    // 再生：无来源纯状态结算
                    case BP_Regen regen:
                        if (regen.Layers > 0) actions_.Add(new BA_Heal(null, self_, regen.Layers));
                        break;
                }
            }
        }

        #endregion
    }
}
