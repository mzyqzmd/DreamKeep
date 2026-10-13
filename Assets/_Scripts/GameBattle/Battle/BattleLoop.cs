using System.Collections.Generic;
using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.GameBattle
{
    /// <summary>战斗循环：以外驱状态机（当前阶段指针 + 切换去重守卫）驱动整场战斗生命周期，也是表现层唯一的命令入口。状态机本身仅持有当前阶段，阶段推进由本循环外层驱动、切状态经宿主路由（如 EndPlayerTurn）。
    /// 循环独占由 RunLoop 自身的守卫保证：启动与入队命令都只可能拉起一条循环，命令一律在等待态串行取出，能量校验与扣减不会被打断。</summary>
    public class BattleLoop
    {
        private readonly StateMachine<BaseState<BattleLoop>> _fsm = new();
        private readonly Queue<BattleCommand> _commandQueue = new();
        private bool _loopRunning; // 循环独占标志：RunLoop 进入即置位、退出即清位

        internal BattleContext BattleContext { get; }
        internal ActionSystem ActionSystem { get; }

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
            BattleContext = battleContext_;
            DrawPerTurn = drawPerTurn_;

            ActionSystem = new ActionSystem(battleContext_, dispatcher_);

            PhaseBattleStart = new BP_BattleStart(this);
            PhasePlayerTurnStart = new BP_PlayerTurnStart(this);
            PhasePlayerTurnActive = new BP_PlayerTurnActive(this);
            PhasePlayerTurnEnd = new BP_PlayerTurnEnd(this);
            PhaseEnemyTurnStart = new BP_EnemyTurnStart(this);
            PhaseEnemyTurnActive = new BP_EnemyTurnActive(this);
            PhaseEnemyTurnEnd = new BP_EnemyTurnEnd(this);
            PhaseBattleFinish = new BP_BattleFinish(this);
        }

        #region 唯一的外部命令入口：只入队不直接驱动，循环在等待态取出执行

        public void EnqueueCommand(BattleCommand command_)
        {
            _commandQueue.Enqueue(command_);
            RunLoop().Forget();
        }

        #endregion

        #region 启动战斗：初始化状态机入口后拉起循环，由该阶段触发开始事件后自动推进

        public void Start()
        {
            _fsm.Initialize(PhaseBattleStart);
            RunLoop().Forget();
        }

        #endregion

        // 循环独占：同一时刻只允许一条在跑，重复调用同步命中守卫后立即返回；
        // 守卫在首个 await 之前同步判定并置位、finally 清位，两者之间没有异步窗口
        private async UniTask RunLoop()
        {
            if (_loopRunning) return;
            _loopRunning = true;
            try
            {
                while (true)
                {
                    BattlePhase current = _fsm.CurrentState as BattlePhase;
                    if (current == null) return;

                    if (current is IBattleCommandReceiver receiver)
                    {
                        if (_commandQueue.Count == 0) return;
                        await receiver.HandleCommand(_commandQueue.Dequeue());
                        continue;
                    }

                    await current.RunAsync();
                    if (BattleContext.IsBattleOver)
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