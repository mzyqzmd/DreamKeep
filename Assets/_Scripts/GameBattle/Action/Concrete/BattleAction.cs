using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>结算动作基类：同步执行并产出领域事件列表，绝不碰表现层。</summary>
    public abstract class BattleAction
    {
        public Creature Source { get; }
        /// <summary>目标在构造时由落点解析确定，动作本身是无状态数据包。</summary>
        public List<Creature> Targets { get; }

        protected BattleAction(Creature source_, List<Creature> targets_)
        {
            Source = source_;
            Targets = targets_;
        }

        /// <summary>由子类实现：同步改战斗态并返回本次产生的领域事件列表。</summary>
        protected abstract List<IBattleEvent> OnExecute(BattleContext battleContext_);

        /// <summary>由动作系统调用，子类不覆写。</summary>
        public List<IBattleEvent> Execute(BattleContext battleContext_) => OnExecute(battleContext_);
    }
}
