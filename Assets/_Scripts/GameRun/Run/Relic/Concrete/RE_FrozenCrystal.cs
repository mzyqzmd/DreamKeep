using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>永冻冰晶：本场首次打出能力牌时获得格挡（每场一次）。</summary>
    public sealed class RE_FrozenCrystal : RelicEffect_T<SO_FrozenCrystal>
    {
        private bool _used;

        public RE_FrozenCrystal(SO_FrozenCrystal data_, BattleContext context_, ActionSystem actions_,
            BattleEventDispatcher dispatcher_, RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            Dispatcher.Register<BE_CardPlayed>(OnCardPlayed);
        }

        private async UniTask OnCardPlayed(BE_CardPlayed ev_)
        {
            if (_used || ev_.Card.Type != ECardType.Ability) return;
            _used = true;
            await PerformAsync(new BA_Block(null, PlayerTarget, Data.Block));
        }
    }
}
