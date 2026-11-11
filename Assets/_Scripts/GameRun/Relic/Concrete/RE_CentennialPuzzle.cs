using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>百年积木：每场战斗中第一次损失生命值时，抽 N 张牌。
    /// 伤害事件的 Amount 是修正后总伤害、Absorbed 是格挡吸收，两者之差大于零才是真的失血；
    /// 放血这类无视格挡的"失去生命"走 BE_HealthLost，同样计为失血。</summary>
    public sealed class RE_CentennialPuzzle : RelicEffect_T<SO_CentennialPuzzle>
    {
        private bool _lostHp; // 每场一次

        public RE_CentennialPuzzle(SO_CentennialPuzzle data_, BattleContext context_, ActionSystem actions_, BattleEventDispatcher dispatcher_,
            RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            Dispatcher.Register<BE_DamageDealt>(OnDamageDealt);
            Dispatcher.Register<BE_HealthLost>(OnHealthLost);
        }

        private async UniTask OnDamageDealt(BE_DamageDealt ev_)
        {
            if (_lostHp || ev_.Target != Context.Player.Creature) return;
            if (ev_.Amount - ev_.Absorbed <= 0) return;
            await DrawAsync();
        }

        private async UniTask OnHealthLost(BE_HealthLost ev_)
        {
            if (_lostHp || ev_.Target != Context.Player.Creature || ev_.Amount <= 0) return;
            await DrawAsync();
        }

        private async UniTask DrawAsync()
        {
            _lostHp = true;
            List<Creature> self = PlayerTarget;
            BattleAction[] draws = new BattleAction[Data.DrawCounts];
            for (int i = 0; i < draws.Length; i++)
                draws[i] = new BA_DrawCard(null, self);
            await PerformAsync(draws);
        }
    }
}
