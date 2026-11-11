using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>先古段删牌/升级结算完成：域侧已写域，特效根据此播演出，演出播完流程才继续。</summary>
    public struct AE_CardsOperationFinished : IAncientEvent
    {
        public IReadOnlyList<CardModel> Cards { get; }

        public bool Upgraded { get; }

        public AE_CardsOperationFinished(IReadOnlyList<CardModel> cards_, bool upgraded_)
        {
            Cards = cards_;
            Upgraded = upgraded_;
        }
    }
}
