using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>古茶具套装：到达休息处后标记下一场战斗，首回合玩家行动开始时额外获得能量并清除标记；
    /// 标记住 Player.RelicCounters，随既有存档链落盘。</summary>
    public sealed class RE_VenerableTeaSet : RelicEffect_T<SO_VenerableTeaSet>
    {
        public RE_VenerableTeaSet(SO_VenerableTeaSet data_, BattleContext context_, ActionSystem actions_,
            BattleEventDispatcher dispatcher_, RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            Dispatcher.Register<BE_PlayerTurnActiveStarted>(OnPlayerTurnActiveStarted);
        }

        private async UniTask OnPlayerTurnActiveStarted(BE_PlayerTurnActiveStarted ev_)
        {
            if (Context.TurnCount != 1) return;
            if (Context.Player.RelicCounterOf(Relic) <= 0) return;

            Context.Player.ResetRelicCounter(Relic);
            Creature self = Context.Player.Creature;
            await PerformAsync(new BA_GainEnergy(self, PlayerTarget, Data.Energy));
        }
    }
}
