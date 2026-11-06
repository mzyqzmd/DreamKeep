namespace _DreamKeep.GameRun
{
    /// <summary>战斗段事实广播器：本段收尾流程与表现视图共用的下行出口，契约固化在 IBattleSegmentEvent；
    /// 实例随战斗段 scope 创建与销毁，不与其它段共享，段卸载后订阅不会残留。</summary>
    public sealed class BattleSegmentEventDispatcher : SegmentEventDispatcher<IBattleSegmentEvent> { }
}
