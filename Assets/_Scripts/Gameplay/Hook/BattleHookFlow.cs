namespace _DreamKeep.Gameplay
{
    /// <summary>流程级钩子基类：仅响应 ctx_.Point，按流程点类型 TPoint 过滤后转发 OnHook。</summary>
    public abstract class BattleHookFlow<TPoint> : BattleHook where TPoint : HookPoint
    {
        public override void Invoke(HookContext ctx_)
        {
            if (ctx_.Point is TPoint point) OnHook(point, ctx_);
        }

        /// <summary>流程级钩子逻辑：point 为订阅的流程点触发器，ctx 携带完整上下文。</summary>
        protected abstract void OnHook(TPoint point_, HookContext ctx_);
    }
}
