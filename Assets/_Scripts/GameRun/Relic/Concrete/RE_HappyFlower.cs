using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>开心小花：每 N 个回合获得能量。
    /// 挂「回合开始」事件（能量已被重置），按回合序取模判定；TurnCount 从 1 起，第 N/2N/…回合触发。</summary>
    public sealed class RE_HappyFlower : RelicEffect<SO_HappyFlower>
    {
        public RE_HappyFlower(SO_HappyFlower data_, BattleContext context_, ActionSystem actions_, BattleEventDispatcher dispatcher_,
            RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            Dispatcher.Register<BE_PlayerTurnActiveStarted>(OnPlayerTurnActiveStarted);
        }

        private async UniTask OnPlayerTurnActiveStarted(BE_PlayerTurnActiveStarted ev_)
        {
            if (Context.TurnCount % Data.TurnInterval != 0) return;
            Creature self = Context.Player.Creature;
            await PerformAsync(new BA_GainEnergy(self, PlayerTarget, Data.Energy));
        }
    }
}
