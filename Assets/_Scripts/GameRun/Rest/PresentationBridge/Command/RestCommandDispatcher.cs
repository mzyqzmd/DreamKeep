using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>休息段上行命令门面：视图只提交语义化意图，不直接触碰流程循环。</summary>
    public sealed class RestCommandDispatcher
    {
        private readonly RestLoop _loop;

        public RestCommandDispatcher(RestLoop loop_) => _loop = loop_;

        /// <summary>点选某个休息选项。</summary>
        public void Choose(int index_) => _loop.Choose(index_);

        /// <summary>点「继续」：每次点击都播一次段完成。</summary>
        public void Proceed() => _loop.Proceed();

        /// <summary>交回锻造/烹饪面板结果：null 表示玩家关闭面板取消，回选项重选。</summary>
        public void SubmitCardSelection(IReadOnlyList<CardModel> cards_) => _loop.SubmitCardSelection(cards_);
    }
}
