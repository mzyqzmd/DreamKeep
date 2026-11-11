using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>金刚杵：战斗开始时给自己施加力量。</summary>
    public sealed class RE_Vajra : RelicEffect
    {
        private readonly BattlePerk _perk;

        public RE_Vajra(SO_Vajra data_, BattleContext context_, ActionSystem actions_, BattleEventDispatcher dispatcher_,
            RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            _perk = PerkBaker.Bake(data_.Strength);
            if (_perk != null) _perk.AddLayers(data_.Layers);
            Dispatcher.Register<BE_BattleStarted>(OnBattleStarted);
        }

        private async UniTask OnBattleStarted(BE_BattleStarted ev_)
        {
            if (_perk == null) return;
            Creature self = Context.Player.Creature;
            await PerformAsync(new BA_ApplyPerk(self, PlayerTarget, _perk.Clone()));
        }
    }
}
