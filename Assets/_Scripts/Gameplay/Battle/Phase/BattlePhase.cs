using _DreamKeep.Utility;

namespace _DreamKeep.Gameplay
{
    /// <summary>战斗阶段基类：阶段逻辑写在 Enter 中，推进一律由 BattleLoop 外层驱动，禁止在 Enter 内切换状态。</summary>
    public abstract class BattlePhase : BaseState<BattleLoop>
    {
        public abstract string PhaseName { get; }
        /// <summary>等待型阶段为 true 时，BattleLoop 停下等命令而不自动推进。</summary>
        public abstract bool IsWaiting { get; }
        /// <summary>下一阶段，由宿主提供，避免阶段之间互相注入。</summary>
        public abstract BaseState<BattleLoop> Next { get; }

        protected BattlePhase(StateMachine<BaseState<BattleLoop>> stateMachine_, BattleLoop owner_)
            : base(stateMachine_, owner_)
        {
        }
    }
}
