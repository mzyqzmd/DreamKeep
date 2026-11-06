namespace _DreamKeep.GameRun
{
    /// <summary>战斗段上行命令门面：视图只提交语义化意图，不直接触碰收尾流程循环。</summary>
    public sealed class BattleSegmentCommandDispatcher
    {
        private readonly BattleSegmentLoop _loop;

        public BattleSegmentCommandDispatcher(BattleSegmentLoop loop_) => _loop = loop_;

        /// <summary>奖励面板「继续」已点：领完放行，段按完成收尾。</summary>
        public void AcknowledgeReward() => _loop.AcknowledgeReward();

        /// <summary>奖励面板弹出失败：降级放行，段按退出收尾。</summary>
        public void SkipReward() => _loop.SkipReward();
    }
}
