using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>状态介入战斗的掉血入口：实际掉血写入生命后询问一次，宿主带「横冲直撞」且生命值不高于层数时
    /// 直接嵌套执行打断动作（只合事件不进队列，照 BA_SettlePerk 先例）。格挡全吸收（hpLoss=0）不会走到这里。</summary>
    internal static class HpDropHook
    {
        #region 掉血后检查横冲直撞阈值：命中即击晕切阶段，产出的事件并入调用方的收集表

        /// <summary>受伤目标带横冲直撞且 CurrentHp ≤ 层数时触发打断；同类型状态唯一，命中即止。</summary>
        internal static void Check(BattleContext context_, Creature target_, List<IBattleEvent> events_)
        {
            if (target_.IsDead) return;

            IReadOnlyList<BattlePerk> perks = target_.Perks;
            for (int i = 0; i < perks.Count; i++)
            {
                if (perks[i] is not BP_Rampage rampage) continue;
                if (target_.CurrentHp > rampage.Layers) continue;

                BA_RampageBreak breakAction = new(target_);
                events_.AddRange(breakAction.Execute(context_));
                break;
            }

            // 狱火：宿主在自己回合内失血时对所有敌人造成群体伤害；纯状态伤害不吃攻击修正也不触发反伤
            if (!context_.IsPlayerTurn) return;
            BP_Inferno inferno = BattlePerkQuery.Find<BP_Inferno>(target_);
            if (inferno == null || inferno.Layers <= 0) return;

            List<Creature> enemies = context_.GetAliveCreatures();
            if (enemies.Count == 0) return;
            events_.AddRange(new BA_Damage(target_, enemies, inferno.Layers, false, false).Execute(context_));
        }

        #endregion
    }
}
