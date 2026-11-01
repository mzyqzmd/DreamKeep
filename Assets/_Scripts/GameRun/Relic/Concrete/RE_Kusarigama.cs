using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>锁镰：每回合每打出 N 张攻击牌，对随机敌人造成伤害；回合开始时重置计数。</summary>
    public sealed class RE_Kusarigama : RelicEffect_T<SO_Kusarigama>
    {
        public RE_Kusarigama(SO_Kusarigama data_, BattleContext context_, ActionSystem actions_,
            BattleEventDispatcher dispatcher_, RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            Dispatcher.Register<BE_TurnStarted>(OnTurnStarted);
            Dispatcher.Register<BE_CardPlayed>(OnCardPlayed);
            NotifyCount(0); // 进战斗即归零显示：计数住效果实例，下场战斗重新累积
        }

        private void OnTurnStarted(BE_TurnStarted ev_)
        {
            if (ev_.IsPlayerTurn) { Counter = 0; NotifyCount(0); }
        }

        private async UniTask OnCardPlayed(BE_CardPlayed ev_)
        {
            if (ev_.Card.Type != ECardType.Attack) return;
            if (++Counter < Data.Count) { NotifyCount(Counter); return; }
            Counter = 0;
            NotifyCount(0);
            await PerformAsync(new BA_DamageRandomEnemy(null, Data.Damage, false));
        }
    }
}
