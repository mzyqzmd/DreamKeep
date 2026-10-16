using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>状态行为「灼烧」：宿主回合末受到等于层数的伤害——无来源纯状态结算（不触发攻击演出）、
    /// 可被格挡吸收、不吃数值修正（显式关闭）。</summary>
    public static class BurnHook
    {
        #region 灼烧在回合末产生"受到层数伤害"的动作

        public static void Contribute(Creature host_, List<Creature> self_, List<BattleAction> actions_)
        {
            int layers = host_.GetPerkLayers<BP_Burn>();
            if (layers > 0) actions_.Add(new BA_Damage(null, self_, layers, false));
        }

        #endregion
    }
}
