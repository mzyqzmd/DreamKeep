using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>烛台：指定回合的玩家回合开始时获得能量（回能之后结算）。</summary>
    public sealed class RE_Candelabra : RelicEffect_T<SO_Candelabra>
    {
        public RE_Candelabra(SO_Candelabra data_, BattleContext context_, ActionSystem actions_, BattleEventDispatcher dispatcher_,
            RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            Dispatcher.Register<BE_PlayerTurnActiveStarted>(OnPlayerTurnActiveStarted);
        }

        private async UniTask OnPlayerTurnActiveStarted(BE_PlayerTurnActiveStarted ev_)
        {
            if (Context.TurnCount != Data.Turn) return;
            Creature self = Context.Player.Creature;
            await PerformAsync(new BA_GainEnergy(self, PlayerTarget, Data.Energy));
        }
    }
}
