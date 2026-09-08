using System.Collections.Generic;
using _DreamKeep.Utility;

namespace _DreamKeep.Gameplay
{
    /// <summary>敌人回合进行阶段：逐个存活敌人把意图词条入队并同步排空，与玩家卡共用同一条管线。</summary>
    public class Phase_EnemyTurnActive : BattlePhase
    {
        public Phase_EnemyTurnActive(StateMachine<BaseState<BattleLoop>> stateMachine_, BattleLoop owner_)
            : base(stateMachine_, owner_)
        {
        }

        public override string PhaseName => "敌人回合进行";
        public override bool IsWaiting => false;
        public override BaseState<BattleLoop> Next => _owner.PhaseEnemyTurnEnd;

        public override void Enter()
        {
            _owner.Events.Emit(new TurnPhaseChangedEvent(PhaseName));
            BattleState state = _owner.BattleState;

            foreach (Enemy enemy in state.GetAliveEnemies())
            {
                List<Actor> self = new List<Actor> { enemy };
                List<Actor> opponents = new List<Actor> { state.Player };
                _owner.AffixFactory.BuildFrom(enemy.Intent, enemy, self, opponents);
                _owner.ActionSystem.PerformAll();
                if (state.Player.IsDead) break;
            }
        }
    }
}
