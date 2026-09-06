using System;

namespace _DreamKeep.Data
{
    /// <summary>落点修饰型词条「指定」：允许拖拽指定单体目标，覆盖默认的最前一个。</summary>
    [Serializable]
    public class AD_Aim : AffixData
    {
        public override string Name => "指定";
        public override string Description => "选定单体目标，覆盖默认的最前一个";
        public override bool ShowDescription => false;
        public override EAffixKind Kind => EAffixKind.Aim;
    }
}
