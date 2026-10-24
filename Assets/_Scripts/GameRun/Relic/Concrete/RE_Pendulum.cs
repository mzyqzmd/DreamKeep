using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>摆动球：每 N 回合抽牌。
    /// 挂「回合开始」事件（初始抽牌已演完），按回合序取模判定；TurnCount 从 1 起，第 N/2N/…回合触发。</summary>
    public sealed class RE_Pendulum : RelicEffect
    {
        private readonly SO_Pendulum _data;

        public RE_Pendulum(SO_Pendulum data_, BattleContext context_, ActionSystem actions_, BattleEventDispatcher dispatcher_,
            RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            _data = data_;
            Dispatcher.Register<BE_PlayerTurnActiveStarted>(OnPlayerTurnActiveStarted);
        }

        private async UniTask OnPlayerTurnActiveStarted(BE_PlayerTurnActiveStarted ev_)
        {
            if (Context.TurnCount % _data.TurnInterval != 0) return;
            Creature self = Context.Player.Creature;
            List<Creature> target = new() { self };
            BattleAction[] draws = new BattleAction[_data.DrawCount];
            for (int i = 0; i < draws.Length; i++)
                draws[i] = new BA_DrawCard(self, target);
            await PerformAsync(draws);
        }
    }
}
