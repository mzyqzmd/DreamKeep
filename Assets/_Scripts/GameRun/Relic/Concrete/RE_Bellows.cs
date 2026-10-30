using System;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>风箱：首回合在初始抽牌发生之前，把抽牌堆顶部 DrawPerTurn 张升级。
    /// 抽牌堆顶即随后会被抽到的初始手牌，视图创建时直接绑定升级后的卡面，不需要另发"已升级"事件。</summary>
    public sealed class RE_Bellows : RelicEffect
    {
        public RE_Bellows(SO_Bellows data_, BattleContext context_, ActionSystem actions_, BattleEventDispatcher dispatcher_,
            RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_) =>
            Dispatcher.Register<BE_TurnStarted>(OnTurnStarted);

        private void OnTurnStarted(BE_TurnStarted ev_)
        {
            if (!ev_.IsPlayerTurn || ev_.TurnCount != 1) return;
            CardPile drawPile = Context.Player.CombatState.DrawPile;
            int count = Math.Min(Context.DrawPerTurn, drawPile.Count);
            for (int i = 0; i < count; i++) drawPile.Cards[i].Upgrade();
        }
    }
}
