namespace _DreamKeep.GameRun
{
    /// <summary>事件段上行命令门面：视图只提交语义化意图，不直接触碰流程循环。</summary>
    public sealed class EventCommandDispatcher
    {
        private readonly EventLoop _loop;

        public EventCommandDispatcher(EventLoop loop_) => _loop = loop_;

        /// <summary>点选某个下标：抉择页选项与遗物二选一共用，当前含义由流程循环自己掌握。</summary>
        public void Choose(int index_) => _loop.Choose(index_);

        /// <summary>点「继续」：结果页之后每次点击都播一次段完成。</summary>
        public void Proceed() => _loop.Proceed();

        /// <summary>奖励面板「继续」已点：放行流程。</summary>
        public void AcknowledgeReward() => _loop.AcknowledgeReward();
    }
}
