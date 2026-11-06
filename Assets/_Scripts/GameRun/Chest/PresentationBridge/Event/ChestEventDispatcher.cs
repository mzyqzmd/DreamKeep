namespace _DreamKeep.GameRun
{
    /// <summary>宝箱段事实广播器：本段规则层与表现视图共用的下行出口，契约固化在 IChestEvent；
    /// 各订阅者初始化时自注册关心的处理器，订阅者只能读事件，不许改事件已表达的结算结果。
    /// 实例随宝箱段 scope 创建与销毁，不与其它段共享，段卸载后订阅不会残留。</summary>
    public sealed class ChestEventDispatcher : SegmentEventDispatcher<IChestEvent> { }
}
