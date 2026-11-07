namespace _DreamKeep.GameRun
{
    /// <summary>整局状态的磁盘快照：三份整局配置只写 GUID，随机流写当前 state，其余是可直接拍平的标量。</summary>
    public sealed class SaveRun
    {
        public string PlayerConfigGuid { get; set; }
        public string MapConfigGuid { get; set; }
        public string FallbackRelicPoolGuid { get; set; }
        public uint RngState { get; set; }
        public int Gold { get; set; }
        public int Floor { get; set; }
        public int CardRemovalsUsed { get; set; }
        public double ElapsedSeconds { get; set; }
        public SavePlayer Player { get; set; }
        public SaveMap Map { get; set; }
    }
}
