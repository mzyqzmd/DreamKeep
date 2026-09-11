using _DreamKeep.Utility;

namespace _DreamKeep.Gameplay
{
    /// <summary>战斗开始阶段：先触发战斗开始钩子，再派发战斗开始事件，随后自动推进到首个玩家回合。</summary>
    public class BP_BattleStart : BattlePhase
    {
        public BP_BattleStart(StateMachine<BaseState<BattleLoop>> stateMachine_, BattleLoop owner_)
            : base(stateMachine_, owner_)
        {
        }

        public override string PhaseName => "战斗开始";
        public override bool IsWaiting => false;
        public override BaseState<BattleLoop> Next => _owner.PhasePlayerTurnStart;

        public override void Enter()
        {
            _owner.Hooks.InvokePoint<HP_BattleStart>(_owner.BattleState);
            _owner.Events.Emit(new IBE_BattleStarted());
        }
    }
}
