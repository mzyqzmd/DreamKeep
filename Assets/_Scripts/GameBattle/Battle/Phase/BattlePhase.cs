using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.GameBattle
{
    /// <summary>战斗阶段基类：阶段逻辑写在 RunAsync 中（含逐动作 await 演出），推进一律由 BattleLoop 外层驱动，禁止在阶段内切换状态。Enter 仅供 StateMachine 调用且默认不做事，避免与 Utility/FSM 共用签名冲突。</summary>
    public abstract class BattlePhase : BaseState<BattleLoop>
    {
        /// <summary>下一阶段，由宿主提供，避免阶段之间互相注入。</summary>
        public abstract BaseState<BattleLoop> Next { get; }

        protected BattlePhase(StateMachine<BaseState<BattleLoop>> stateMachine_, BattleLoop owner_)
            : base(stateMachine_, owner_)
        {
        }

        /// <summary>阶段主逻辑：由 BattleLoop 在切换后 await 驱动，可 await 动作演出。</summary>
        public virtual UniTask RunAsync() => UniTask.CompletedTask;

        /// <summary>阶段事件统一播放：本地收集的事件经 ActionSystem 交给表现层 await。</summary>
        protected UniTask PlayEvents(params IBattleEvent[] events_)
            => _owner.ActionSystem.PlayAsync(events_);
    }
}