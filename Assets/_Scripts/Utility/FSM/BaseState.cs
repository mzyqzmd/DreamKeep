namespace _DreamKeep.Utility
{
    /// <summary>
    /// 状态基类，T 为宿主类型（如 BattleLoop）。
    /// </summary>
    public abstract class BaseState<T> : IState where T : class
    {
        protected T _owner;

        protected BaseState(T owner_)
        {
            _owner = owner_;
        }

        public virtual void Enter() { }
        public virtual void Exit() { }
    }
}
