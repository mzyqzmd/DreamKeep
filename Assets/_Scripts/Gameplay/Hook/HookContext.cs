namespace _DreamKeep.Gameplay
{
    /// <summary>统一触发上下文：动作级与流程级钩子共享同一载荷，并补上动作级原缺失的战斗态与动作队列。</summary>
    public sealed class HookContext
    {
        public BattleState BattleState { get; }
        public BattleAction Action { get; }        // 动作级钩子非空，流程级为 null
        public HookPoint Point { get; }            // 流程级钩子非空，动作级为 null
        public bool IsPre { get; }                 // 仅动作级有意义
        public bool IsPost { get; }                // 仅动作级有意义
        public ActionSystem ActionSystem { get; }  // 供 Post 钩子 Enqueue 新动作，流程级触发时为 null

        public HookContext(BattleState battleState_, ActionSystem actionSystem_, BattleAction action_ = null, HookPoint point_ = null, bool isPre_ = false, bool isPost_ = false)
        {
            BattleState = battleState_;
            ActionSystem = actionSystem_;
            Action = action_;
            Point = point_;
            IsPre = isPre_;
            IsPost = isPost_;
        }

    }
}
