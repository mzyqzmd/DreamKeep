namespace _DreamKeep.GameBattle
{
    /// <summary>抽牌被拒：手牌已达上限，本张不入手牌、抽牌堆不动，也不触发洗回。
    /// 逐张抽牌各自判定、各自广播；当前无消费者，供将来「手牌已满」提示直接挂接。</summary>
    public struct BE_CardDrawRejected : IBattleEvent
    {
    }
}
