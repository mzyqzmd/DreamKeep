namespace _DreamKeep.Core
{
    /// <summary>
    /// 状态基类，T 为宿主类型（如 Player、Enemy）。
    /// </summary>
    public abstract class BaseState<T> : IState where T : class
    {
        protected StateMachine<BaseState<T>> _fsm;
        protected T _owner;

        protected BaseState(StateMachine<BaseState<T>> stateMachine_, T owner_)
        {
            _fsm = stateMachine_;
            _owner = owner_;
        }

        public virtual void Enter() { }
        public virtual void Update() { }
        public virtual void Exit() { }
    }
}
