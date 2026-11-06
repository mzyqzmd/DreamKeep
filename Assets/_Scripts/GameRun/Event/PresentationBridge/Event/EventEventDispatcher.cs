namespace _DreamKeep.GameRun
{
    /// <summary>事件段事实广播器：本段规则层与表现视图共用的下行出口，契约固化在 IEventSegmentEvent；
    /// 实例随事件段 scope 创建与销毁，不与其它段共享，段卸载后订阅不会残留。</summary>
    public sealed class EventEventDispatcher : SegmentEventDispatcher<IEventSegmentEvent> { }
}
