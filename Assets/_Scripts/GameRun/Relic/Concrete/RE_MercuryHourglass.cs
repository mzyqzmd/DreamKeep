using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>水银沙漏：每个玩家回合开始时对全体敌人造成伤害，遗物伤害不吃力量与易伤。</summary>
    public sealed class RE_MercuryHourglass : RelicEffect
    {
        private readonly SO_MercuryHourglass _data;

        public RE_MercuryHourglass(SO_MercuryHourglass data_, BattleContext context_, ActionSystem actions_,
            BattleEventDispatcher dispatcher_, RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            _data = data_;
            Dispatcher.Register<BE_PlayerTurnActiveStarted>(OnPlayerTurnActiveStarted);
        }

        private async UniTask OnPlayerTurnActiveStarted(BE_PlayerTurnActiveStarted ev_)
        {
            List<Creature> targets = new();
            foreach (Enemy enemy in Context.GetAliveEnemies()) targets.Add(enemy.Creature);
            if (targets.Count == 0) return;
            await PerformAsync(new BA_Damage(null, targets, _data.Damage, false));
        }
    }
}
