using System;
using _DreamKeep.ConfigData;

namespace _DreamKeep.GameRun
{
    /// <summary>整局流程规则：节点类型到段种类的映射。
    /// 纯静态、零状态、不认识场景名与面板；进段后的场景分派与收段由表现层执行。</summary>
    public static class RunFlow
    {
        #region 节点类型 → 段种类：问号揭示后按真实类型分派

        public static ESegmentKind SegmentKindOf(EMapPointType type_) => type_ switch
        {
            EMapPointType.Monster => ESegmentKind.Battle,
            EMapPointType.Elite => ESegmentKind.Battle,
            EMapPointType.Boss => ESegmentKind.Battle,
            EMapPointType.Ancient => ESegmentKind.Ancient,
            EMapPointType.Shop => ESegmentKind.Shop,
            EMapPointType.Rest => ESegmentKind.Rest,
            EMapPointType.Treasure => ESegmentKind.Treasure,
            EMapPointType.Event => ESegmentKind.Event,
            _ => throw new ArgumentOutOfRangeException(nameof(type_), type_, "节点类型没有对应段种类")
        };

        #endregion
    }
}
