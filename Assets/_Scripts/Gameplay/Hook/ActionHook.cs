namespace _DreamKeep.Gameplay
{
    /// <summary>动作级钩子基类：按动作类型过滤后转发，订阅者只需实现 OnHook。</summary>
    public abstract class ActionHook<T> : IActionHook where T : GameAction
    {
        public void Invoke(GameAction action_)
        {
            if (action_ is T typed)
                OnHook(typed);
        }

        /// <summary>Pre 阶段可改动作字段或取消；Post 阶段只读，但可往队列追加新动作。</summary>
        public abstract void OnHook(T action_);
    }
}
