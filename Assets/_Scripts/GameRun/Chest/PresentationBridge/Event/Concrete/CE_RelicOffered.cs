using _DreamKeep.ConfigData;

namespace _DreamKeep.GameRun
{
    /// <summary>遗物候选就位：表现层据此在中心生成待选槽位；领取失败重开选择时再播一次。</summary>
    public struct CE_RelicOffered : IChestEvent
    {
        public SO_Relic Relic { get; }

        public CE_RelicOffered(SO_Relic relic_) => Relic = relic_;
    }
}
