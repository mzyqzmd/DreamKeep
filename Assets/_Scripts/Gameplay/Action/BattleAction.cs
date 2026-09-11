using System.Collections.Generic;

namespace _DreamKeep.Gameplay
{
    /// <summary>结算动作基类：同步执行，改 BattleState 并写事件缓冲，绝不碰表现层。</summary>
    public abstract class BattleAction
    {
        /// <summary>被 Pre 钩子取消后置位，Execute 阶段跳过。</summary>
        public bool IsCanceled { get; private set; }
        public Actor Source { get; }
        /// <summary>目标在构造时由落点解析确定，动作本身是无状态数据包。</summary>
        public List<Actor> Targets { get; }

        protected BattleAction(Actor source_, List<Actor> targets_)
        {
            Source = source_;
            Targets = targets_;
        }

        /// <summary>供 Pre 钩子取消本次动作。</summary>
        public void Cancel() => IsCanceled = true;

        /// <summary>由子类实现：同步改战斗态并写事件，不做任何异步等待。</summary>
        protected abstract void OnExecute(BattleState battleState_, BattleEventBuffer events_);

        /// <summary>由动作系统调用，子类不覆写。</summary>
        public void Execute(BattleState battleState_, BattleEventBuffer events_) => OnExecute(battleState_, events_);
    }
}
