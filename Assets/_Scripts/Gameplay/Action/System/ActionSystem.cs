using System.Collections.Generic;

namespace _DreamKeep.Gameplay
{
    /// <summary>动作系统：动作队列 + 同步排空，取代一次只跑一个动作且并发时丢动作的旧实现。</summary>
    public class ActionSystem
    {
        private readonly Queue<BattleAction> _queue = new();
        private readonly BattleState _battleState;
        private readonly HookRegistry _hooks;
        private readonly BattleEventBuffer _events;

        public bool IsPerforming { get; private set; }

        public ActionSystem(BattleState battleState_, HookRegistry hooks_, BattleEventBuffer events_)
        {
            _battleState = battleState_;
            _hooks = hooks_;
            _events = events_;
        }

        #region 入队一个动作，队列不设上限且永不丢动作

        public void Enqueue(BattleAction action_) => _queue.Enqueue(action_);

        #endregion

        #region 同步排空整个队列，Post 钩子追加的动作会在本轮继续出队

        public void PerformAll()
        {
            if (IsPerforming) return;
            IsPerforming = true;
            while (_queue.Count > 0)
                Resolve(_queue.Dequeue());
            IsPerforming = false;
        }

        #endregion

        #region 单个动作的三时机执行：Pre 可改可取消，Execute 改状态，Post 只追加

        private void Resolve(BattleAction action_)
        {
            _hooks.InvokeActionPre(action_, _battleState, this);
            if (action_.IsCanceled) return;
            action_.Execute(_battleState, _events);
            _hooks.InvokeActionPost(action_, _battleState, this);
        }

        #endregion
    }
}