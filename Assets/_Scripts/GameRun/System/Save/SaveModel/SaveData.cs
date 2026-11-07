using System.Collections.Generic;

namespace _DreamKeep.GameRun
{
    /// <summary>一份存档的顶层 DTO：版本号、段上下文、整局快照与待领奖励清单。
    /// SegmentEntryRngState 只在 AwaitingReward 有意义，供读档时局部重放开段装配还原背景 / 遭遇 / 牌序。</summary>
    public sealed class SaveData
    {
        public int SchemaVersion { get; set; }
        public ESaveSegmentContext SegmentContext { get; set; }
        public long SaveTime { get; set; }
        public uint SegmentEntryRngState { get; set; }
        public SaveRun Run { get; set; }
        public List<SaveReward> PendingRewards { get; set; } = new();
    }
}
