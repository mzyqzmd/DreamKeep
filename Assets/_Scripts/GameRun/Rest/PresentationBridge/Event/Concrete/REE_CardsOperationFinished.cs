using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>休息段锻造/烹饪结算完成：域侧已写域，特效根据此播演出，演出播完流程才继续。</summary>
    public struct REE_CardsOperationFinished : IRestEvent
    {
        public IReadOnlyList<CardModel> Cards { get; }

        public bool Upgraded { get; }

        public REE_CardsOperationFinished(IReadOnlyList<CardModel> cards_, bool upgraded_)
        {
            Cards = cards_;
            Upgraded = upgraded_;
        }
    }
}
