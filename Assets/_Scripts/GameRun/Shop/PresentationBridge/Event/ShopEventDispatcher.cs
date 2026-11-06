namespace _DreamKeep.GameRun
{
    /// <summary>商店段事实广播器：本段规则层与表现视图共用的下行出口，契约固化在 IShopEvent；
    /// 实例随商店段 scope 创建与销毁，不与其它段共享，段卸载后订阅不会残留。</summary>
    public sealed class ShopEventDispatcher : SegmentEventDispatcher<IShopEvent> { }
}
