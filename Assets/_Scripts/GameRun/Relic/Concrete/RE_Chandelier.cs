using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>吊灯：指定回合的玩家回合开始时获得能量（回能之后结算）。</summary>
    public sealed class RE_Chandelier : RelicEffect
    {
        private readonly SO_Chandelier _data;

        public RE_Chandelier(SO_Chandelier data_, BattleContext context_, ActionSystem actions_, BattleEventDispatcher dispatcher_,
            RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            _data = data_;
            Dispatcher.Register<BE_PlayerTurnActiveStarted>(OnPlayerTurnActiveStarted);
        }

        private async UniTask OnPlayerTurnActiveStarted(BE_PlayerTurnActiveStarted ev_)
        {
            if (Context.TurnCount != _data.Turn) return;
            Creature self = Context.Player.Creature;
            await PerformAsync(new BA_GainEnergy(self, new List<Creature> { self }, _data.Energy));
        }
    }
}
