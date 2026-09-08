using System.Collections.Generic;
using _DreamKeep.Utility;

namespace _DreamKeep.Gameplay
{
    /// <summary>玩家回合开始阶段：触发回合开始钩子，回满能量并逐张抽牌。</summary>
    public class Phase_PlayerTurnStart : BattlePhase
    {
        public Phase_PlayerTurnStart(StateMachine<BaseState<BattleLoop>> stateMachine_, BattleLoop owner_)
            : base(stateMachine_, owner_)
        {
        }

        public override string PhaseName => "玩家回合开始";
        public override bool IsWaiting => false;
        public override BaseState<BattleLoop> Next => _owner.PhasePlayerTurnActive;

        public override void Enter()
        {
            _owner.Events.Emit(new TurnPhaseChangedEvent(PhaseName));
            _owner.Hooks.InvokePoint<HP_TurnStart>(_owner.BattleState);

            Player player = _owner.BattleState.Player;
            List<Actor> self = new List<Actor> { player };
            ActionSystem actions = _owner.ActionSystem;

            actions.Enqueue(new Action_ResetEnergy(player, self, _owner.BattleState.EnergyPerTurn));
            for (int i = 0; i < _owner.DrawPerTurn; i++)
                actions.Enqueue(new Action_DrawCard(player, self));
            actions.PerformAll();
        }
    }
}
