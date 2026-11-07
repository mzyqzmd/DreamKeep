namespace _DreamKeep.GameBattle
{
    /// <summary>抽牌溢出：手牌已达上限，本次抽到的牌直接进弃牌堆；视图据此让玩家说一句。
    /// 与既有的 BE_CardDiscarded 并行发出，后者继续负责弃牌堆计数。</summary>
    public struct BE_HandOverflowed : IBattleEvent
    {
    }
}
