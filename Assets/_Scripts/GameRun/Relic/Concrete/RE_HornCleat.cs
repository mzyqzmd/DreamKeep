using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>船夹板：指定回合的玩家回合开始时获得格挡（清格挡之后结算）。</summary>
    public sealed class RE_HornCleat : RelicEffect
    {
        private readonly SO_HornCleat _data;

        public RE_HornCleat(SO_HornCleat data_, BattleContext context_, ActionSystem actions_, BattleEventDispatcher dispatcher_,
            RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            _data = data_;
            Dispatcher.Register<BE_PlayerTurnActiveStarted>(OnPlayerTurnActiveStarted);
        }

        private async UniTask OnPlayerTurnActiveStarted(BE_PlayerTurnActiveStarted ev_)
        {
            if (Context.TurnCount != _data.Turn) return;
            await PerformAsync(new BA_Block(null, new List<Creature> { Context.Player.Creature }, _data.Block));
        }
    }
}
