using System.Collections.Generic;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.GameBattle
{
    /// <summary>战斗循环：以状态机驱动整场战斗的生命周期，也是表现层唯一的命令入口。演出期间屏蔽输入，防止动画未播完时再次驱动战斗。</summary>
    public class BattleLoop
    {
        private readonly StateMachine<BaseState<BattleLoop>> _fsm = new();
        private readonly BattleContext _battleContext;
        private readonly ActionSystem _actionSystem;
        private readonly Queue<BattleCommand> _commandQueue = new();
        private bool _loopRunning;

        internal BattleContext BattleContext => _battleContext;
        internal ActionSystem ActionSystem => _actionSystem;
        internal int DrawPerTurn { get; }

        internal BattlePhase PhaseBattleStart { get; }
        internal BattlePhase PhasePlayerTurnStart { get; }
        internal BattlePhase PhasePlayerTurnActive { get; }
        internal BattlePhase PhasePlayerTurnEnd { get; }
        internal BattlePhase PhaseEnemyTurnStart { get; }
        internal BattlePhase PhaseEnemyTurnActive { get; }
        internal BattlePhase PhaseEnemyTurnEnd { get; }
        internal BattlePhase PhaseBattleFinish { get; }

        public BattleLoop(BattleContext battleContext_, int drawPerTurn_, BattleEventDispatcher dispatcher_)
        {
            _battleContext = battleContext_;
            DrawPerTurn = drawPerTurn_;

            _actionSystem = new ActionSystem(battleContext_, dispatcher_);

            PhaseBattleStart = new BP_BattleStart(_fsm, this);
            PhasePlayerTurnStart = new BP_PlayerTurnStart(_fsm, this);
            PhasePlayerTurnActive = new BP_PlayerTurnActive(_fsm, this);
            PhasePlayerTurnEnd = new BP_PlayerTurnEnd(_fsm, this);
            PhaseEnemyTurnStart = new BP_EnemyTurnStart(_fsm, this);
            PhaseEnemyTurnActive = new BP_EnemyTurnActive(_fsm, this);
            PhaseEnemyTurnEnd = new BP_EnemyTurnEnd(_fsm, this);
            PhaseBattleFinish = new BP_BattleFinish(_fsm, this);
        }

        #region 唯一的外部命令入口：只入队不直接驱动，循环在等待态取出执行

        public void EnqueueCommand(BattleCommand command_)
        {
            _commandQueue.Enqueue(command_);
            if (_loopRunning) return;
            _loopRunning = true;
            RunLoop().Forget();
        }

        #endregion

        #region 启动战斗：进入战斗开始阶段，由该阶段触发开始事件后自动推进

        public async UniTask Start()
        {
            _fsm.Initialize(PhaseBattleStart);
            await RunLoop();
        }

        #endregion

        private async UniTask RunLoop()
        {
            try
            {
                while (true)
                {
                    BattlePhase current = _fsm.CurrentState as BattlePhase;
                    if (current == null) return;

                    if (current.IsWaiting)
                    {
                        if (_commandQueue.Count == 0) return;
                        BattleCommand command = _commandQueue.Dequeue();
                        await current.HandleCommand(command);
                        continue;
                    }

                    await current.RunAsync();
                    if (_battleContext.IsBattleOver)
                    {
                        _fsm.ChangeState(PhaseBattleFinish);
                        await PhaseBattleFinish.RunAsync();
                        return;
                    }

                    _fsm.ChangeState(current.Next);
                }
            }
            finally
            {
                _loopRunning = false;
            }
        }

        #region 玩家结束回合：供 BP_PlayerTurnActive 在命令分派时切到结束阶段，随后由主循环推进

        internal void EndPlayerTurn() => _fsm.ChangeState(PhasePlayerTurnEnd);

        #endregion
    }
}
