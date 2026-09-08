using System;
using System.Collections.Generic;

namespace _DreamKeep.Utility
{
    /// <summary>
    /// 单层状态机，状态转换由状态内部调用 ChangeState 自决，不提供分层子状态机与表驱动条件转换。
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

        public void Update() => CurrentState?.Update();
    }
}
