using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>请求玩家自选升级卡牌：视图弹升级板，选中列表或取消经结果源交回。</summary>
    public struct EE_UpgradeCardsRequested : IEventSegmentEvent
    {
        public int Count { get; }

        public UniTaskCompletionSource<IReadOnlyList<CardModel>> Result { get; }

        public EE_UpgradeCardsRequested(int count_)
        {
            Count = count_;
            Result = new UniTaskCompletionSource<IReadOnlyList<CardModel>>();
        }
    }
}
