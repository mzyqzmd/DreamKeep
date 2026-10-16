using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>状态行为「再生」：宿主回合末恢复等于层数的生命——无来源纯状态结算，事件只带目标。</summary>
    public static class RegenHook
    {
        #region 再生在回合末产生"恢复层数生命"的动作

        public static void Contribute(Creature host_, List<Creature> self_, List<BattleAction> actions_)
        {
            int layers = host_.GetPerkLayers<BP_Regen>();
            if (layers > 0) actions_.Add(new BA_Heal(null, self_, layers));
        }

        #endregion
    }
}
