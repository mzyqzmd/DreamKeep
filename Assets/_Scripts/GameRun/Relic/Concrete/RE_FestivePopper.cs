using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>节日拉炮：战斗开始时对全体敌人造成伤害，遗物伤害不吃力量与易伤。</summary>
    public sealed class RE_FestivePopper : RelicEffect
    {
        private readonly SO_FestivePopper _data;

        public RE_FestivePopper(SO_FestivePopper data_, BattleContext context_, ActionSystem actions_,
            BattleEventDispatcher dispatcher_, RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            _data = data_;
            Dispatcher.Register<BE_BattleStarted>(OnBattleStarted);
        }

        private async UniTask OnBattleStarted(BE_BattleStarted ev_)
        {
            List<Creature> targets = new();
            foreach (Enemy enemy in Context.GetAliveEnemies()) targets.Add(enemy.Creature);
            if (targets.Count == 0) return;
            await PerformAsync(new BA_Damage(null, targets, _data.Damage, false));
        }
    }
}
