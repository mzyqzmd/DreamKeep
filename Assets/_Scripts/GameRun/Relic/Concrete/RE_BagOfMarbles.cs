using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>弹珠袋：战斗开始时给全体敌人施加易伤。</summary>
    public sealed class RE_BagOfMarbles : RelicEffect
    {
        private readonly BattlePerk _perk;

        public RE_BagOfMarbles(SO_BagOfMarbles data_, BattleContext context_, ActionSystem actions_, BattleEventDispatcher dispatcher_,
            RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            _perk = PerkBaker.Bake(data_.Vulnerable);
            if (_perk != null) _perk.AddLayers(data_.Layers);
            Dispatcher.Register<BE_BattleStarted>(OnBattleStarted);
        }

        private async UniTask OnBattleStarted(BE_BattleStarted ev_)
        {
            if (_perk == null) return;
            List<Creature> targets = new();
            foreach (Enemy enemy in Context.GetAliveEnemies()) targets.Add(enemy.Creature);
            if (targets.Count == 0) return;
            await PerformAsync(new BA_ApplyPerk(Context.Player.Creature, targets, _perk.Clone()));
        }
    }
}
