using System;

namespace _DreamKeep.Data
{
    /// <summary>保留词条：标记型。带此词条的牌回合结束不进弃牌堆，仅当被打出时才离场。</summary>
    [Serializable]
    public class AD_Retain : AffixData
    {
        public override string Name => "保留";
        public override string Description => "回合结束不会进入弃牌堆";
        public override bool ShowDescription => false;
        public override EAffixKind Kind => EAffixKind.Retain;
    }
}
