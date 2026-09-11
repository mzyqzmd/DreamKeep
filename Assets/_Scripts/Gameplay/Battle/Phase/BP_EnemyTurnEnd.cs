using System.Collections.Generic;
using _DreamKeep.Utility;

namespace _DreamKeep.Gameplay
{
    /// <summary>敌人回合结束阶段：敌人状态衰减并推进回合计数，一回合到此结束。</summary>
    public class BP_EnemyTurnEnd : BattlePhase
    {
        public BP_EnemyTurnEnd(StateMachine<BaseState<BattleLoop>> stateMachine_, BattleLoop owner_)
            : base(stateMachine_, owner_)
        {
        }

        public override string PhaseName => "敌人回合结束";
        public override bool IsWaiting => false;
        public override BaseState<BattleLoop> Next => _owner.PhasePlayerTurnStart;

        public override void Enter()
        {
            _owner.Events.Emit(new IBE_TurnPhaseChanged(PhaseName));
            BattleState state = _owner.BattleState;
            ActionSystem actions = _owner.ActionSystem;

            foreach (Enemy enemy in state.Enemies)
            {
                if (enemy.IsDead) continue;
                List<Actor> self = new List<Actor> { enemy };
                actions.Enqueue(new BA_DecayPerk(enemy, self));
            }

            actions.PerformAll();

            // 唯一例外：回合计数无来源无目标且不发事件，不做成动作，直接推进
            state.TurnCount++;
            _owner.Hooks.InvokePoint<HP_TurnEnd>(state);
        }
    }
}
