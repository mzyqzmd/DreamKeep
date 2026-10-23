using System.Collections.Generic;

namespace _DreamKeep.Utility
{
    /// <summary>
    /// 单层状态机，仅持有当前状态并提供切换去重守卫；状态转换由外部持有者（如 BattleLoop）或宿主路由驱动，不内嵌帧驱动与自决切换逻辑。
    /// </summary>
    public class StateMachine<TState> where TState : IState
    {
        /// <summary>当前状态</summary>
        public TState CurrentState { get; private set; }

        public void Initialize(TState state_)
        {
            CurrentState = state_;
            CurrentState?.Enter();
        }

        public void ChangeState(TState newState_)
        {
            if (EqualityComparer<TState>.Default.Equals(newState_, CurrentState)) return;
            CurrentState.Exit();
            CurrentState = newState_;
            CurrentState.Enter();
        }
    }
}
