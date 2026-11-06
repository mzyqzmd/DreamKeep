namespace _DreamKeep.GameRun
{
    /// <summary>段内异常报告：域层不碰引擎日志，由表现入口统一写 Console，保持与迁移前一致的报错口径。</summary>
    public struct AE_ErrorReported : IAncientEvent
    {
        public string Message { get; }

        public AE_ErrorReported(string message_) => Message = message_;
    }
}
