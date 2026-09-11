namespace _DreamKeep.Gameplay
{
    /// <summary>动作级钩子基类：仅响应 ctx_.Action，按动作类型 TAction 过滤后转发 OnHook。</summary>
    public abstract class BattleHookAction<TAction> : BattleHook where TAction : BattleAction
    {
        public override void Invoke(HookContext ctx_)
        {
            if (ctx_.Action is TAction action) OnHook(action, ctx_);
        }

        /// <summary>动作级钩子逻辑：action 为订阅的动作触发器，ctx 携带完整上下文。</summary>
        protected abstract void OnHook(TAction action_, HookContext ctx_);
    }
}
