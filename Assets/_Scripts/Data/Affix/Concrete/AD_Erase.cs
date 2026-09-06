using System;

namespace _DreamKeep.Data
{
    /// <summary>遗忘词条：标记型。带此词条的卡被打出时，从整局游戏（run）永久移除自身（区别于「消耗」仅本场离场）。</summary>
    [Serializable]
    public class AD_Erase : AffixData
    {
        public override string Name => "遗忘";
        public override string Description => "打出时从整局游戏永久移除自身";
        public override bool ShowDescription => false;
        public override EAffixKind Kind => EAffixKind.Erase;
    }
}
