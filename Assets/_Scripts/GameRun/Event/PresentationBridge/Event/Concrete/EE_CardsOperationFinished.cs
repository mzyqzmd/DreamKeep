using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>事件段删牌/升级结算完成：域侧已写域，特效根据此播演出，演出播完流程才继续。</summary>
    public struct EE_CardsOperationFinished : IEventSegmentEvent
    {
        public IReadOnlyList<CardModel> Cards { get; }

        public bool Upgraded { get; }

        public EE_CardsOperationFinished(IReadOnlyList<CardModel> cards_, bool upgraded_)
        {
            Cards = cards_;
            Upgraded = upgraded_;
        }
    }
}
