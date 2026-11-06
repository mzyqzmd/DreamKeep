namespace _DreamKeep.GameRun
{
    /// <summary>宝箱段上行命令门面：视图只提交语义化意图，不直接触碰流程循环。</summary>
    public sealed class ChestCommandDispatcher
    {
        private readonly ChestLoop _loop;

        public ChestCommandDispatcher(ChestLoop loop_) => _loop = loop_;

        public void OpenChest() => _loop.OpenChest();

        public void ClaimRelic() => _loop.ClaimRelic();

        public void Proceed() => _loop.Proceed();
    }
}
