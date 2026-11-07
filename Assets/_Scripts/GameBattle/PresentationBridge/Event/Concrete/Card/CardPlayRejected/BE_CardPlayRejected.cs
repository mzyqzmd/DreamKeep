using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>出牌被拒：能量不足、牌已不在手牌等任何原因导致命令未被执行，卡未进入打出堆；
    /// 视图据此把停在出牌区的牌退回手牌槽位，点结束回合的阶段对账仍是兜底；Reason 供表现层决定是否让玩家开口。</summary>
    public struct BE_CardPlayRejected : IBattleEvent
    {
        public CardModel Card { get; }
        public ECardPlayRejectReason Reason { get; }

        public BE_CardPlayRejected(CardModel card_, ECardPlayRejectReason reason_)
        {
            Card = card_;
            Reason = reason_;
        }
    }
}
