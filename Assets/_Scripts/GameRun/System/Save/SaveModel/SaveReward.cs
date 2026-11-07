using System.Collections.Generic;

namespace _DreamKeep.GameRun
{
    /// <summary>一条待领奖励的磁盘形态：读哪几个字段由 Kind 决定，与 RunReward 的四个工厂一一对应。</summary>
    public sealed class SaveReward
    {
        public ERewardKind Kind { get; set; }
        public int Gold { get; set; }
        public List<SaveCard> Cards { get; set; } = new();
        public string PotionGuid { get; set; }
        public string RelicGuid { get; set; }
    }
}
