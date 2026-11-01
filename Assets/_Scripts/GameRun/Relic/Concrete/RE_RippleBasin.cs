using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>波纹水盆：本回合未打出攻击牌时，回合结束获得格挡。</summary>
    public sealed class RE_RippleBasin : RelicEffect_T<SO_RippleBasin>
    {
        private bool _attacked;

        public RE_RippleBasin(SO_RippleBasin data_, BattleContext context_, ActionSystem actions_,
            BattleEventDispatcher dispatcher_, RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            Dispatcher.Register<BE_CardPlayed>(OnCardPlayed);
            Dispatcher.Register<BE_PlayerTurnActiveStarted>(OnPlayerTurnActiveStarted);
            Dispatcher.Register<BE_PlayerTurnEnded>(OnPlayerTurnEnded);
        }

        private void OnCardPlayed(BE_CardPlayed ev_)
        {
            if (ev_.Card.Type == ECardType.Attack) _attacked = true;
        }

        private void OnPlayerTurnActiveStarted(BE_PlayerTurnActiveStarted ev_) => _attacked = false;

        private async UniTask OnPlayerTurnEnded(BE_PlayerTurnEnded ev_)
        {
            if (_attacked) return;
            await PerformAsync(new BA_Block(null, PlayerTarget, Data.Block));
        }
    }
}
