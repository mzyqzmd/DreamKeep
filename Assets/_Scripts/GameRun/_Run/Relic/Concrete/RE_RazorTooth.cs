using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>剃牙刀：每回合首次打出的攻击或技能牌获得升级（本场有效，不写回局内卡组）。
    /// 升级只改卡的运行时数据，本次已建队的结算动作不受影响。</summary>
    public sealed class RE_RazorTooth : RelicEffect
    {
        private bool _used; // 每回合一次：本回合是否已经升级过一张牌

        public RE_RazorTooth(SO_RazorTooth data_, BattleContext context_, ActionSystem actions_,
            BattleEventDispatcher dispatcher_, RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            Dispatcher.Register<BE_TurnStarted>(OnTurnStarted);
            Dispatcher.Register<BE_CardPlayed>(OnCardPlayed);
        }

        private void OnTurnStarted(BE_TurnStarted ev_)
        {
            if (ev_.IsPlayerTurn) _used = false;
        }

        private void OnCardPlayed(BE_CardPlayed ev_)
        {
            if (_used) return;
            if (ev_.Card.Type is not (ECardType.Attack or ECardType.Skill)) return;
            _used = true;
            ev_.Card.Upgrade();
        }
    }
}
