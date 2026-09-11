using _DreamKeep.Utility;

namespace _DreamKeep.Gameplay
{
    /// <summary>敌人回合进行阶段：敌人行动的唯一编排位，当前敌人无行动能力，阶段为空转占位。</summary>
    public class BP_EnemyTurnActive : BattlePhase
    {
        public BP_EnemyTurnActive(StateMachine<BaseState<BattleLoop>> stateMachine_, BattleLoop owner_)
            : base(stateMachine_, owner_)
        {
        }

        public override string PhaseName => "敌人回合进行";
        public override bool IsWaiting => false;
        public override BaseState<BattleLoop> Next => _owner.PhaseEnemyTurnEnd;

        // 敌人 AI 接入点：将来在此遍历存活敌人，把意图经 AffixToActionBaker 入队后 PerformAll
        public override void Enter() => _owner.Events.Emit(new IBE_TurnPhaseChanged(PhaseName));
    }
}
