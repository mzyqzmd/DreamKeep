using _DreamKeep.EngineUtility;

namespace _DreamKeep.Presentation
{
    /// <summary>休息段末的「继续」按钮：段入口在休息/锻造收束后激活并订阅 Clicked，
    /// 每点一次调 SegmentSceneManager.CompleteSegment 开地图并保留本段。点击上报能力继承自 UI_Button。</summary>
    public sealed class UI_RestProceedButton : UI_Button
    {
    }
}
