using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>开信刀：每回合每打出 N 张技能牌，对全体敌人造成伤害；回合开始时重置计数。</summary>
    public sealed class RE_LetterOpener : RelicEffect_T<SO_LetterOpener>
    {
        public RE_LetterOpener(SO_LetterOpener data_, BattleContext context_, ActionSystem actions_,
            BattleEventDispatcher dispatcher_, RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            Dispatcher.Register<BE_TurnStarted>(OnTurnStarted);
            Dispatcher.Register<BE_CardPlayed>(OnCardPlayed);
            NotifyCount(0); // 进战斗即归零显示：计数住效果实例，下场战斗重新累积
        }

        private void OnTurnStarted(BE_TurnStarted ev_)
        {
            if (ev_.IsPlayerTurn) { Counter = 0; NotifyCount(0); }
        }

        private async UniTask OnCardPlayed(BE_CardPlayed ev_)
        {
            if (ev_.Card.Type != ECardType.Skill) return;
            if (++Counter < Data.Count) { NotifyCount(Counter); return; }
            Counter = 0;
            NotifyCount(0);
            List<Creature> targets = new();
            foreach (Enemy enemy in Context.GetAliveEnemies()) targets.Add(enemy.Creature);
            if (targets.Count == 0) return;
            await PerformAsync(new BA_Damage(null, targets, Data.Damage, false));
        }
    }
}
