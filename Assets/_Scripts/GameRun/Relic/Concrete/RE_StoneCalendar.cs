using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>历石：指定回合的回合结束时对全体敌人造成伤害（遗物伤害不吃力量与易伤）。</summary>
    public sealed class RE_StoneCalendar : RelicEffect_T<SO_StoneCalendar>
    {
        public RE_StoneCalendar(SO_StoneCalendar data_, BattleContext context_, ActionSystem actions_,
            BattleEventDispatcher dispatcher_, RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            Dispatcher.Register<BE_PlayerTurnEnded>(OnPlayerTurnEnded);
        }

        private async UniTask OnPlayerTurnEnded(BE_PlayerTurnEnded ev_)
        {
            if (Context.TurnCount != Data.Turn) return;
            List<Creature> targets = new();
            foreach (Enemy enemy in Context.GetAliveEnemies()) targets.Add(enemy.Creature);
            if (targets.Count == 0) return;
            await PerformAsync(new BA_Damage(null, targets, Data.Damage, false));
        }
    }
}
