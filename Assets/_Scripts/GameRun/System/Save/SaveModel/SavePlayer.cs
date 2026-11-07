using System.Collections.Generic;

namespace _DreamKeep.GameRun
{
    /// <summary>玩家的整局快照：生命、遗物、三格药水（空槽记 null）、局内卡组与跨节点遗物计数。
    /// 战斗态不进档——段入口与结算态都不在战斗进行中。</summary>
    public sealed class SavePlayer
    {
        public int CurrentHp { get; set; }
        public int MaxHp { get; set; }
        public List<string> RelicGuids { get; set; } = new();
        public List<string> PotionSlotGuids { get; set; } = new();
        public List<SaveCard> Deck { get; set; } = new();
        public Dictionary<string, int> RelicCounters { get; set; } = new();
    }
}
