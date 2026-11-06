using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>手钻：一次伤害把敌人格挡打空（有吸收且结算后格挡为 0）时，给该敌人施加易伤。</summary>
    public sealed class RE_HandDrill : RelicEffect
    {
        private readonly BattlePerk _perk;

        public RE_HandDrill(SO_HandDrill data_, BattleContext context_, ActionSystem actions_, BattleEventDispatcher dispatcher_,
            RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            _perk = PerkBaker.Bake(data_.Vulnerable);
            if (_perk != null) _perk.AddLayers(data_.Layers);
            Dispatcher.Register<BE_DamageDealt>(OnDamageDealt);
        }

        private async UniTask OnDamageDealt(BE_DamageDealt ev_)
        {
            if (_perk == null) return;
            if (ev_.Target == Context.Player.Creature || ev_.Target.IsDead) return;
            if (ev_.Absorbed <= 0 || ev_.Target.Block > 0) return;
            await PerformAsync(new BA_ApplyPerk(Context.Player.Creature, new List<Creature> { ev_.Target }, _perk.Clone()));
        }
    }
}
