using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>面包：首回合少获得能量，其余回合每回合额外获得能量（回能之后结算）。</summary>
    public sealed class RE_Bread : RelicEffect
    {
        private readonly SO_Bread _data;

        public RE_Bread(SO_Bread data_, BattleContext context_, ActionSystem actions_, BattleEventDispatcher dispatcher_,
            RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            _data = data_;
            Dispatcher.Register<BE_PlayerTurnActiveStarted>(OnPlayerTurnActiveStarted);
        }

        private async UniTask OnPlayerTurnActiveStarted(BE_PlayerTurnActiveStarted ev_)
        {
            Creature self = Context.Player.Creature;
            List<Creature> target = new() { self };
            await PerformAsync(Context.TurnCount == 1
                ? new BA_SpendEnergy(self, target, _data.FirstTurnPenalty)
                : new BA_GainEnergy(self, target, _data.Energy));
        }
    }
}
