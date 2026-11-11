using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>恶魔之舌：每回合首次失去生命时，回复等量生命（失血量＝伤害减格挡吸收）。
    /// 放血这类无视格挡的"失去生命"走 BE_HealthLost，同样计入。</summary>
    public sealed class RE_DemonTongue : RelicEffect
    {
        private bool _healed;

        public RE_DemonTongue(SO_DemonTongue data_, BattleContext context_, ActionSystem actions_,
            BattleEventDispatcher dispatcher_, RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            Dispatcher.Register<BE_TurnStarted>(OnTurnStarted);
            Dispatcher.Register<BE_DamageDealt>(OnDamageDealt);
            Dispatcher.Register<BE_HealthLost>(OnHealthLost);
        }

        // 只在玩家回合开始重置：BE_TurnStarted 在敌方回合同样会发，无守卫会变成"每个半回合回复一次"
        private void OnTurnStarted(BE_TurnStarted ev_)
        {
            if (ev_.IsPlayerTurn) _healed = false;
        }

        private async UniTask OnDamageDealt(BE_DamageDealt ev_)
        {
            if (_healed || ev_.Target != Context.Player.Creature) return;
            int lost = ev_.Amount - ev_.Absorbed;
            if (lost <= 0) return;
            _healed = true;
            await PerformAsync(new BA_Heal(null, new List<Creature> { ev_.Target }, lost));
        }

        private async UniTask OnHealthLost(BE_HealthLost ev_)
        {
            if (_healed || ev_.Target != Context.Player.Creature || ev_.Amount <= 0) return;
            _healed = true;
            await PerformAsync(new BA_Heal(null, new List<Creature> { ev_.Target }, ev_.Amount));
        }
    }
}
