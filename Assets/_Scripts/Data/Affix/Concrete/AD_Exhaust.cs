using System;

namespace _DreamKeep.Data
{
    /// <summary>消耗词条：标记型。带此词条的卡打出后进入消耗区，本场战斗不再被抽到（战斗结束重洗后回牌组）。</summary>
    [Serializable]
    public class AD_Exhaust : AffixData
    {
        public override string Name => "消耗";
        public override bool ShowDescription => false;
    }
}
