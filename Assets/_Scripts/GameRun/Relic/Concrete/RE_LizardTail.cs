using System;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>蜥蜴尾巴：每场战斗第一次受到致命伤害时，把生命改为最大生命的指定比例，充当"免死"。
    /// 拦截发生在写血之前，血量从不归零，故不会产生死亡事实；_used 随效果实例每战新建，天然逐场重置。</summary>
    public sealed class RE_LizardTail : RelicEffect<SO_LizardTail>, IDeathGuard
    {
        private bool _used; // 每场一次

        public RE_LizardTail(SO_LizardTail data_, BattleContext context_, ActionSystem actions_,
            BattleEventDispatcher dispatcher_, RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
        }

        #region 致命伤拦截：只保护玩家，拦下即播报生效与用尽

        public bool TryPreventDeath(BattleContext context_, Creature target_, int hpLoss_, out int hpAfter_)
        {
            hpAfter_ = 0;
            if (_used || target_ != Context.Player.Creature) return false;
            _used = true;
            hpAfter_ = Math.Max(1, (int)(target_.MaxHp * Data.HealPercent));
            Notifier.NotifyTriggered(Relic); // 涟漪与本次伤害同帧
            Notifier.NotifyExhausted(Relic); // 槽位定格为灰
            return true;
        }

        #endregion
    }
}
