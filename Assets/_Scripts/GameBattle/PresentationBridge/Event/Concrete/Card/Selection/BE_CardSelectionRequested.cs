using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>手牌选择请求：域侧已判明存在取舍并算好候选，表现层只陈列并把选中列表交回完成源。</summary>
    public struct BE_CardSelectionRequested : IBattleEvent
    {
        public ECardSelectMode Mode { get; }
        public int TargetCount { get; }
        public IReadOnlyList<CardModel> Candidates { get; }
        public Creature Source { get; }
        public List<Creature> Targets { get; }
        public UniTaskCompletionSource<IReadOnlyList<CardModel>> Result { get; }

        public BE_CardSelectionRequested(ECardSelectMode mode_, int targetCount_,
            IReadOnlyList<CardModel> candidates_, Creature source_, List<Creature> targets_)
        {
            Mode = mode_;
            TargetCount = targetCount_;
            Candidates = candidates_;
            Source = source_;
            Targets = targets_;
            Result = new UniTaskCompletionSource<IReadOnlyList<CardModel>>();
        }
    }
}
