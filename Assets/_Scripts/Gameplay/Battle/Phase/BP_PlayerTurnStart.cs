using System.Collections.Generic;
using _DreamKeep.Utility;

namespace _DreamKeep.Gameplay
{
    /// <summary>玩家回合开始阶段：触发回合开始钩子，回满能量并逐张抽牌。</summary>
    public class BP_PlayerTurnStart : BattlePhase
    {
        public BP_PlayerTurnStart(StateMachine<BaseState<BattleLoop>> stateMachine_, BattleLoop owner_)
            : base(stateMachine_, owner_)
        {
        }

        public override string PhaseName => "玩家回合开始";
        public override bool IsWaiting => false;
        public override BaseState<BattleLoop> Next => _owner.PhasePlayerTurnActive;

        public override void Enter()
        {
            _owner.Events.Emit(new IBE_TurnPhaseChanged(PhaseName));
            _owner.Hooks.InvokePoint<HP_TurnStart>(_owner.BattleState);

            Player player = _owner.BattleState.Player;
            List<Actor> self = new List<Actor> { player };
            ActionSystem actions = _owner.ActionSystem;

            // 先落盾再回能：玩家上回合残留的格挡不跨回合保留
            actions.Enqueue(new BA_ClearBlock(player, self));
            actions.Enqueue(new BA_ResetEnergy(player, _owner.BattleState.EnergyPerTurn.Value));
            for (int i = 0; i < _owner.DrawPerTurn; i++)
                actions.Enqueue(new BA_DrawCard(player, self));
            actions.PerformAll();
        }
    }
}