using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>锻造自选请求：视图弹升级板，选中列表或取消经结果源交回。</summary>
    public struct REE_UpgradeCardsRequested : IRestEvent
    {
        public int Count { get; }

        public UniTaskCompletionSource<IReadOnlyList<CardModel>> Result { get; }

        public REE_UpgradeCardsRequested(int count_)
        {
            Count = count_;
            Result = new UniTaskCompletionSource<IReadOnlyList<CardModel>>();
        }
    }
}
