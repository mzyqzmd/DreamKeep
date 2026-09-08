using System.Collections.Generic;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;

namespace _DreamKeep.Gameplay
{
    /// <summary>玩家回合结束阶段：结算灼烧与再生，触发回合结束钩子，弃掉未保留的手牌并衰减状态。</summary>
    public class Phase_PlayerTurnEnd : BattlePhase
    {
        public Phase_PlayerTurnEnd(StateMachine<BaseState<BattleLoop>> stateMachine_, BattleLoop owner_)
            : base(stateMachine_, owner_)
        {
        }

        public override string PhaseName => "玩家回合结束";
        public override bool IsWaiting => false;
        public override BaseState<BattleLoop> Next => _owner.PhaseEnemyTurnStart;

        public override void Enter()
        {
            _owner.Events.Emit(new TurnPhaseChangedEvent(PhaseName));
            BattleState state = _owner.BattleState;
            Player player = state.Player;
            if (player.IsDead) return;

            List<Actor> self = new List<Actor> { player };
            ActionSystem actions = _owner.ActionSystem;

            // 灼烧属受到伤害：可被格挡吸收，但不吃力量/虚弱/易伤修正
            int burn = player.GetPerkLayers<IBP_Burn>();
            if (burn > 0)
                actions.Enqueue(new Action_Damage(player, self, burn) { IgnoresModifiers = true });

            int regen = player.GetPerkLayers<IBP_Regen>();
            if (regen > 0)
                actions.Enqueue(new Action_Heal(player, self, regen));

            actions.PerformAll();

            _owner.Hooks.InvokePoint<HP_TurnEnd>(state);

            actions.Enqueue(new Action_DiscardHand(player, self));
            actions.Enqueue(new Action_DecayStatus(player, self));
            actions.PerformAll();
        }
    }
}
