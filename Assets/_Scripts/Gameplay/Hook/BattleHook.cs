namespace _DreamKeep.Gameplay
{
    /// <summary>钩子统一非泛型基类：钩子逻辑按触发器类别分两种（动作级 BattleHookAction / 流程级 BattleHookFlow），由 HookRegistry 以 List&lt;BattleHook&gt; 统一持有与分发。</summary>
    public abstract class BattleHook
    {
        /// <summary>由注册表在分发时调用，按触发器类型过滤后转发到具体钩子的 OnHook。</summary>
        public abstract void Invoke(HookContext ctx_);
    }
}
