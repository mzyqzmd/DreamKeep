using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>卡戎之灰：每消耗一张牌，对全体敌人造成伤害（遗物伤害不吃力量与易伤）。</summary>
    public sealed class RE_CharonsAshes : RelicEffect<SO_CharonsAshes>
    {
        public RE_CharonsAshes(SO_CharonsAshes data_, BattleContext context_, ActionSystem actions_,
            BattleEventDispatcher dispatcher_, RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            Dispatcher.Register<BE_CardExhausted>(OnCardExhausted);
        }

        private async UniTask OnCardExhausted(BE_CardExhausted ev_)
        {
            List<Creature> targets = new();
            foreach (Enemy enemy in Context.GetAliveEnemies()) targets.Add(enemy.Creature);
            if (targets.Count == 0) return;
            await PerformAsync(new BA_Damage(null, targets, Data.Damage, false));
        }
    }
}
