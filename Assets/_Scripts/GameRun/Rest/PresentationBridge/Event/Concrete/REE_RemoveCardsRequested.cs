using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>烹饪自选请求：视图弹删牌板，选中列表或取消经结果源交回。</summary>
    public struct REE_RemoveCardsRequested : IRestEvent
    {
        public int Count { get; }

        public UniTaskCompletionSource<IReadOnlyList<CardModel>> Result { get; }

        public REE_RemoveCardsRequested(int count_)
        {
            Count = count_;
            Result = new UniTaskCompletionSource<IReadOnlyList<CardModel>>();
        }
    }
}
