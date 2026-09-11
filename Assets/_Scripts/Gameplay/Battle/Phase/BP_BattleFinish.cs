using _DreamKeep.Utility;

namespace _DreamKeep.Gameplay
{
    /// <summary>战斗结束终态：按胜负触发对应钩子，按被遗忘卡组编号幂等回写卡组，随后派发战斗结束事件并静默停驻。</summary>
    public class BP_BattleFinish : BattlePhase
    {
        public BP_BattleFinish(StateMachine<BaseState<BattleLoop>> stateMachine_, BattleLoop owner_)
            : base(stateMachine_, owner_)
        {
        }

        public override string PhaseName => "战斗结束";
        public override bool IsWaiting => true;
        public override BaseState<BattleLoop> Next => null;

        public override void Enter()
        {
            if (_owner.BattleState.IsVictory)
                _owner.Hooks.InvokePoint<HP_BattleVictory>(_owner.BattleState);
            else
                _owner.Hooks.InvokePoint<HP_BattleDefeat>(_owner.BattleState);

            // 遗忘是打出即永久移除，与胜负无关，故不论胜败都回写卡组，RemoveCard 幂等
            foreach (int sourceId in _owner.BattleState.ErasedSourceIds)
                _owner.DeckService.RemoveCard(sourceId);

            _owner.Events.Emit(new IBE_BattleEnded(_owner.BattleState.IsVictory));
        }
    }
}
