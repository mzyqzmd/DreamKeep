using System.Collections.Generic;
using _DreamKeep.ConfigData;
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
        private bool _finished; // 终态一次性标志：结束阶段跑完后置位，此后入队命令一律丢弃

        internal BattleContext BattleContext { get; }
        public ActionSystem ActionSystem { get; } // GameRun 的遗物效果装配时要向动作系统提交动作，公开给上游程序集
        internal BattleEventDispatcher EventDispatcher { get; }

        /// <summary>终态已跑完（含结束阶段演出）：整局层的段启动器据此收段重建，不另设回调。</summary>
        public bool IsFinished => _finished;

        /// <summary>本场是否打赢：整局层在 IsFinished 之后据此决定要不要结算奖励。
        /// 胜负只看敌人是否全部阵亡，故战斗态被清空后照样回答得出。</summary>
        public bool IsVictory => BattleContext.IsVictory;

        internal int DrawPerTurn { get; }

        internal BattlePhase PhaseBattleStart { get; }
        internal BattlePhase PhasePlayerTurnStart { get; }
        internal BattlePhase PhasePlayerTurnActive { get; }
        internal BattlePhase PhasePlayerTurnEnd { get; }
        internal BattlePhase PhaseEnemyTurnStart { get; }
        internal BattlePhase PhaseEnemyTurnActive { get; }
        internal BattlePhase PhaseEnemyTurnEnd { get; }
        internal BattlePhase PhaseBattleFinish { get; }

        public BattleLoop(BattleContext battleContext_, int drawPerTurn_, BattleEventDispatcher eventDispatcher_)
        {
            BattleContext = battleContext_;
            DrawPerTurn = drawPerTurn_;
            EventDispatcher = eventDispatcher_;

            ActionSystem = new ActionSystem(battleContext_, eventDispatcher_);

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
            // 终态后丢弃命令：结束阶段已结算过，再入队会重跑它，二次广播战斗结束并重复清空战斗态
            if (_finished) return;
            _commandQueue.Enqueue(command_);
            RunLoop().Forget();
        }

        #endregion

        #region 丢弃药水：任意阶段立即生效（无战斗效果、无演出，不排队也不占动作串行）

        internal void DiscardPotionNow(int slotIndex_)
        {
            if (!BattleContext.Player.TryTakePotion(slotIndex_, out SO_Potion potion)) return;
            EventDispatcher.PlayAsync(new BE_PotionConsumed(slotIndex_, potion)).Forget();
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
        // 守卫在首个 await 之前同步判定并置位、finally 清位，两者之间没有异步窗口。
        // 两类阶段两种推进方式：
        //   等待型（接收器）——一次只处理一条命令，处理完仍留在本阶段等下一轮；它的推进由命令自己经宿主路由完成（如 EndPlayerTurn），
        //     不能读 Next：玩家回合的 Next 是"回合结束"，若每次出牌后都读它，一轮出牌就会把回合结束掉；
        //   驱动型——跑完 RunAsync 后用 Next 回答去处，null 即终态（结束阶段独占此表达）。
        // 胜负在两条路径处理完之后统一判定，故"用卡打死最后一只敌人"当帧即进结束阶段，不必等玩家再点结束回合。
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
                        if (BattleContext.IsBattleOver) _fsm.ChangeState(PhaseBattleFinish);
                        continue;
                    }

                    await current.RunAsync();
                    BaseState<BattleLoop> next = current.Next;
                    if (next == null)
                    {
                        _finished = true; // 终态：置位后 EnqueueCommand 一律早退，结束阶段不会被后续入队重跑
                        return;
                    }
                    _fsm.ChangeState(BattleContext.IsBattleOver ? PhaseBattleFinish : next);
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