using System;

namespace _DreamKeep.Data
{
    /// <summary>落点修饰型词条「穿透」：攻势从落点起点一路贯通到最后一个目标。</summary>
    [Serializable]
    public class AD_Pierce : AffixData
    {
        public override string Name => "穿透";
        public override string Description => "从起始目标到最后一个目标作为目标";
        public override bool ShowDescription => false;
        public override EAffixKind Kind => EAffixKind.Pierce;
    }
}
