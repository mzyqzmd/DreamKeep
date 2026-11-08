using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>孙子兵法：本回合未打出攻击牌，则下个玩家回合开始时获得能量。
    /// 回合末落标记、下回合开窗时兑现；未打攻击的判定与兑现跨一个回合，故用两个字段。</summary>
    public sealed class RE_ArtOfWar : RelicEffect_T<SO_ArtOfWar>
    {
        private bool _attacked;
        private bool _bonusPending;

        public RE_ArtOfWar(SO_ArtOfWar data_, BattleContext context_, ActionSystem actions_, BattleEventDispatcher dispatcher_,
            RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            Dispatcher.Register<BE_CardPlayed>(OnCardPlayed);
            Dispatcher.Register<BE_PlayerTurnEnded>(OnPlayerTurnEnded);
            Dispatcher.Register<BE_PlayerTurnActiveStarted>(OnPlayerTurnActiveStarted);
        }

        private void OnCardPlayed(BE_CardPlayed ev_)
        {
            if (ev_.Card.Type == ECardType.Attack) _attacked = true;
        }

        private void OnPlayerTurnEnded(BE_PlayerTurnEnded ev_)
        {
            _bonusPending = !_attacked;
            _attacked = false;
        }

        private async UniTask OnPlayerTurnActiveStarted(BE_PlayerTurnActiveStarted ev_)
        {
            if (!_bonusPending) return;
            _bonusPending = false;
            Creature self = Context.Player.Creature;
            await PerformAsync(new BA_GainEnergy(self, PlayerTarget, Data.Energy));
        }
    }
}
