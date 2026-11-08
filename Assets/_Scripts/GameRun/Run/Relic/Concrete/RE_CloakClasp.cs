using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>斗篷扣：回合结束时按当前手牌张数获得格挡（此时弃牌尚未执行）。</summary>
    public sealed class RE_CloakClasp : RelicEffect_T<SO_CloakClasp>
    {
        public RE_CloakClasp(SO_CloakClasp data_, BattleContext context_, ActionSystem actions_,
            BattleEventDispatcher dispatcher_, RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            Dispatcher.Register<BE_PlayerTurnEnded>(OnPlayerTurnEnded);
        }

        private async UniTask OnPlayerTurnEnded(BE_PlayerTurnEnded ev_)
        {
            int block = Context.Player.CombatState.Hand.Count * Data.BlockPerCard;
            if (block <= 0) return;
            await PerformAsync(new BA_Block(null, PlayerTarget, block));
        }
    }
}
