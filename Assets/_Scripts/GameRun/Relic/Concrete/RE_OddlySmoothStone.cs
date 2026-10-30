using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>意外光滑的石头：战斗开始时给自己施加敏捷。</summary>
    public sealed class RE_OddlySmoothStone : RelicEffect
    {
        private readonly BattlePerk _perk;

        public RE_OddlySmoothStone(SO_OddlySmoothStone data_, BattleContext context_, ActionSystem actions_,
            BattleEventDispatcher dispatcher_, RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            _perk = PerkBaker.Bake(data_.Dexterity);
            if (_perk != null) _perk.AddLayers(data_.Layers);
            Dispatcher.Register<BE_BattleStarted>(OnBattleStarted);
        }

        private async UniTask OnBattleStarted(BE_BattleStarted ev_)
        {
            if (_perk == null) return;
            Creature self = Context.Player.Creature;
            await PerformAsync(new BA_ApplyPerk(self, new List<Creature> { self }, _perk.Clone()));
        }
    }
}
