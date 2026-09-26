using System;
using System.Collections.Generic;

namespace _DreamKeep.Utility
{
    /// <summary>
    /// 单层状态机，仅持有当前状态并提供切换去重守卫与切换通知；状态转换由外部持有者（如 BattleLoop）或宿主路由驱动，不内嵌帧驱动与自决切换逻辑。
    /// </summary>
    public class StateMachine<TState> where TState : IState
    {
        /// <summary>当前状态</summary>
        public TState CurrentState { get; private set; }
        /// <summary>状态切换事件，参数为(切换前状态, 切换后状态)</summary>
        public Action<TState, TState> OnStateChanged;

        public void Initialize(TState state_)
        {
            CurrentState = state_;
            CurrentState?.Enter();
        }

        public void ChangeState(TState newState_)
        {
            if (EqualityComparer<TState>.Default.Equals(newState_, CurrentState)) return;
            TState from = CurrentState;
            CurrentState.Exit();
            CurrentState = newState_;
            CurrentState.Enter();
            OnStateChanged?.Invoke(from, newState_);
        }
    }
}
