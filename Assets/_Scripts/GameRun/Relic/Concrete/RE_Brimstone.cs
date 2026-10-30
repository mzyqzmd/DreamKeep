using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>硫磺：每个玩家回合开始时自己获得力量，全体敌人也获得力量。</summary>
    public sealed class RE_Brimstone : RelicEffect
    {
        private readonly BattlePerk _selfPerk;
        private readonly BattlePerk _enemyPerk;

        public RE_Brimstone(SO_Brimstone data_, BattleContext context_, ActionSystem actions_, BattleEventDispatcher dispatcher_,
            RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            _selfPerk = PerkBaker.Bake(data_.Strength);
            if (_selfPerk != null) _selfPerk.AddLayers(data_.SelfLayers);
            _enemyPerk = PerkBaker.Bake(data_.Strength);
            if (_enemyPerk != null) _enemyPerk.AddLayers(data_.EnemyLayers);
            Dispatcher.Register<BE_PlayerTurnActiveStarted>(OnPlayerTurnActiveStarted);
        }

        private async UniTask OnPlayerTurnActiveStarted(BE_PlayerTurnActiveStarted ev_)
        {
            if (_selfPerk == null && _enemyPerk == null) return;
            Creature self = Context.Player.Creature;
            List<BattleAction> actions = new();
            if (_selfPerk != null) actions.Add(new BA_ApplyPerk(self, new List<Creature> { self }, _selfPerk.Clone()));

            List<Creature> enemies = new();
            foreach (Enemy enemy in Context.GetAliveEnemies()) enemies.Add(enemy.Creature);
            if (_enemyPerk != null && enemies.Count > 0)
                actions.Add(new BA_ApplyPerk(self, enemies, _enemyPerk.Clone()));

            if (actions.Count == 0) return;
            await PerformAsync(actions.ToArray());
        }
    }
}
