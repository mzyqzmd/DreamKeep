using _DreamKeep.Utility;

namespace _DreamKeep.Gameplay
{
    /// <summary>战斗循环：六阶段状态机的宿主与外层驱动器，也是表现层唯一的命令入口。</summary>
    public class BattleLoop
    {
        // 六阶段一圈最多 6 次切换，取两倍作为推进上限，防止某个阶段漏设 IsWaiting 时锁死主线程
        private const int MaxAdvanceSteps = 12;

        private readonly StateMachine<BaseState<BattleLoop>> _fsm = new();
        private readonly BattleState _battleState;
        private readonly ActionSystem _actionSystem;
        private readonly HookRegistry _hooks;
        private readonly DomainEventBuffer _events;
        private readonly AffixActionFactory _affixFactory;

        public BattleState BattleState => _battleState;
        public ActionSystem ActionSystem => _actionSystem;
        public HookRegistry Hooks => _hooks;
        public DomainEventBuffer Events => _events;
        public AffixActionFactory AffixFactory => _affixFactory;
        public int DrawPerTurn { get; }
        /// <summary>是否停在等待命令的阶段，表现层据此决定能否发命令。</summary>
        public bool IsWaitingForCommand => _fsm.CurrentState is Phase_PlayerTurnActive;

        public BattlePhase PhasePlayerTurnStart { get; }
        public BattlePhase PhasePlayerTurnActive { get; }
        public BattlePhase PhasePlayerTurnEnd { get; }
        public BattlePhase PhaseEnemyTurnStart { get; }
        public BattlePhase PhaseEnemyTurnActive { get; }
        public BattlePhase PhaseEnemyTurnEnd { get; }

        public BattleLoop(BattleState battleState_, HookRegistry hooks_, DomainEventBuffer events_, int drawPerTurn_)
        {
            _battleState = battleState_;
            _hooks = hooks_;
            _events = events_;
            DrawPerTurn = drawPerTurn_;

            _actionSystem = new ActionSystem(battleState_, hooks_, events_);
            _affixFactory = new AffixActionFactory(_actionSystem, new TargetResolver());

            PhasePlayerTurnStart = new Phase_PlayerTurnStart(_fsm, this);
            PhasePlayerTurnActive = new Phase_PlayerTurnActive(_fsm, this);
            PhasePlayerTurnEnd = new Phase_PlayerTurnEnd(_fsm, this);
            PhaseEnemyTurnStart = new Phase_EnemyTurnStart(_fsm, this);
            PhaseEnemyTurnActive = new Phase_EnemyTurnActive(_fsm, this);
            PhaseEnemyTurnEnd = new Phase_EnemyTurnEnd(_fsm, this);
        }

        #region 启动战斗：先触发战斗开始钩子，再进入首个阶段开始推进
        public void Start()
        {
            _hooks.InvokePoint<HP_BattleStart>(_battleState);
            _fsm.Initialize(PhasePlayerTurnStart);
            Advance();
        }
        #endregion

        #region 命令入口：表现层唯一的驱动方式，命令合法性由 Domain 校验
        public void ExecuteCommand(BattleCommand command_)
        {
            if (_battleState.IsBattleOver) return;

            switch (command_)
            {
                case BC_PlayCard playCard:
                    if (!(_fsm.CurrentState is Phase_PlayerTurnActive active)) return;
                   // active.PlayCard(playCard.CardInstanceId, playCard.AimSlot);
                    if (_battleState.IsBattleOver)
                        FinishBattle();
                    break;
                case BC_PlayerEndTurn _:
                    if (!(_fsm.CurrentState is Phase_PlayerTurnActive)) return;
                    _fsm.ChangeState(PhasePlayerTurnEnd);
                    Advance();
                    break;
            }
        }
        #endregion

        // 外层推进：同步跑完非等待阶段，遇等待阶段或战斗结束停下
        private void Advance()
        {
            for (int step = 0; step < MaxAdvanceSteps; step++)
            {
                if (_battleState.IsBattleOver)
                {
                    FinishBattle();
                    return;
                }
                BattlePhase current = _fsm.CurrentState as BattlePhase;
                if (current == null || current.IsWaiting) return;
                _fsm.ChangeState(current.Next);
            }
        }

        private void FinishBattle()
        {
            if (_battleState.IsVictory)
                _hooks.InvokePoint<HP_BattleVictory>(_battleState);
            _events.Emit(new BattleEndedEvent(_battleState.IsVictory));
        }
    }
}
