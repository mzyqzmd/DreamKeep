using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>开心小花：每 N 个回合获得能量；计数整局累计、跨战斗保留（住 Player 计数表）。
    /// 挂「回合开始」事件（能量已被重置），整局回合数 +1 推进，凑满 N 即触发并归零重新累积。</summary>
    public sealed class RE_HappyFlower : RelicEffect_T<SO_HappyFlower>
    {
        public RE_HappyFlower(SO_HappyFlower data_, BattleContext context_, ActionSystem actions_, BattleEventDispatcher dispatcher_,
            RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            Dispatcher.Register<BE_PlayerTurnActiveStarted>(OnPlayerTurnActiveStarted);
            NotifyCount(Context.Player.RelicCounterOf(Relic)); // 进战斗显示上场保留的计数值
        }

        private async UniTask OnPlayerTurnActiveStarted(BE_PlayerTurnActiveStarted ev_)
        {
            int count = Context.Player.AdvanceRelicCounter(Relic);
            if (count < Data.TurnInterval) { NotifyCount(count); return; }
            Context.Player.ResetRelicCounter(Relic);
            NotifyCount(0);
            Creature self = Context.Player.Creature;
            await PerformAsync(new BA_GainEnergy(self, PlayerTarget, Data.Energy));
        }
    }
}
