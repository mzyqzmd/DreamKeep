using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>手牌选择请求：域侧已判明存在取舍并算好候选，表现层只陈列并把选中列表交回完成源。
    /// Scope 是选牌约束的唯一真源：Exact 必须选满 TargetCount，UpTo 可 0~TargetCount，
    /// Any 任选若干（TargetCount 取候选数作为顶替上限）。</summary>
    public struct BE_CardSelectionRequested : IBattleEvent
    {
        public ECardSelectMode Mode { get; }
        public ESelectScope Scope { get; }
        public int TargetCount { get; }
        public IReadOnlyList<CardModel> Candidates { get; }
        public Creature Source { get; }
        public List<Creature> Targets { get; }
        public UniTaskCompletionSource<IReadOnlyList<CardModel>> Result { get; }

        public BE_CardSelectionRequested(ECardSelectMode mode_, ESelectScope scope_, int targetCount_,
            IReadOnlyList<CardModel> candidates_, Creature source_, List<Creature> targets_)
        {
            Mode = mode_;
            Scope = scope_;
            // 任意档没有固定张数，统一以候选数作答量顶替上限，调用方无需各自换算
            TargetCount = scope_ == ESelectScope.Any ? candidates_.Count : targetCount_;
            Candidates = candidates_;
            Source = source_;
            Targets = targets_;
            Result = new UniTaskCompletionSource<IReadOnlyList<CardModel>>();
        }
    }
}
