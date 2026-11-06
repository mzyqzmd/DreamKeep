using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>先古之民段上行命令门面：视图只提交语义化意图，不直接触碰流程循环。</summary>
    public sealed class AncientCommandDispatcher
    {
        private readonly AncientLoop _loop;

        public AncientCommandDispatcher(AncientLoop loop_) => _loop = loop_;

        /// <summary>点选三选一中的某件遗物。</summary>
        public void Choose(int index_) => _loop.Choose(index_);

        /// <summary>交回强制选牌结果：null 表示取消，流程会重弹直到玩家选完。</summary>
        public void SubmitCardSelection(IReadOnlyList<CardModel> cards_) => _loop.SubmitCardSelection(cards_);
    }
}
