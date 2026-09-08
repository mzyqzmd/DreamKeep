using System.Collections.Generic;
using _DreamKeep.Utility;

namespace _DreamKeep.Gameplay
{
    /// <summary>敌人回合开始阶段：清空敌人上回合残留的格挡，为意图结算做准备。</summary>
    public class Phase_EnemyTurnStart : BattlePhase
    {
        public Phase_EnemyTurnStart(StateMachine<BaseState<BattleLoop>> stateMachine_, BattleLoop owner_)
            : base(stateMachine_, owner_)
        {
        }

        public override string PhaseName => "敌人回合开始";
        public override bool IsWaiting => false;
        public override BaseState<BattleLoop> Next => _owner.PhaseEnemyTurnActive;

        public override void Enter()
        {
            _owner.Events.Emit(new TurnPhaseChangedEvent(PhaseName));
            ActionSystem actions = _owner.ActionSystem;

            foreach (Enemy enemy in _owner.BattleState.Enemies)
            {
                if (enemy.IsDead) continue;
                List<Actor> self = new List<Actor> { enemy };
                actions.Enqueue(new Action_ClearBlock(enemy, self));
            }

            actions.PerformAll();
        }
    }
}
