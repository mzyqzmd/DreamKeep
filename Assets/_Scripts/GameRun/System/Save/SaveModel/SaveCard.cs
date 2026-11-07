namespace _DreamKeep.GameRun
{
    /// <summary>一张卡的磁盘形态：只记配置 GUID 与是否升级，其余全部由 CardModelBaker 烘焙还原。</summary>
    public sealed class SaveCard
    {
        public string CardGuid { get; set; }
        public bool IsUpgraded { get; set; }
    }
}
