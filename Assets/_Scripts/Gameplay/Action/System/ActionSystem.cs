using System.Collections.Generic;
using _DreamKeep.RuntimeData;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.Gameplay
{
    /// <summary>动作系统：动作队列 + 异步串行排空，每个动作执行完后等待表现层演出完成再进下一个。</summary>
    public class ActionSystem
    {
        private readonly Queue<BattleAction> _queue = new();
        private readonly BattleContext _battleContext;
        private readonly BattleEventDispatcher _presenter;

        public bool IsPerforming { get; private set; }

        public ActionSystem(BattleContext battleContext_, BattleEventDispatcher presenter_ = null)
        {
            _battleContext = battleContext_;
            _presenter = presenter_;
        }

        #region 入队一个动作，队列不设上限且永不丢动作

        public void Enqueue(BattleAction action_) => _queue.Enqueue(action_);

        #endregion

        #region 异步串行排空：每动作执行后 await 表现层播放，Post 钩子追加的动作会在本轮继续出队

        public async UniTask PerformAllAsync()
        {
            if (IsPerforming) return;
            IsPerforming = true;
            while (_queue.Count > 0)
                await ResolveAsync(_queue.Dequeue());
            IsPerforming = false;
        }

        #endregion

        #region 单个动作执行：Execute 改状态并返回事件，随后 await 演出

        private async UniTask ResolveAsync(BattleAction action_)
        {
            List<IBattleEvent> events = action_.Execute(_battleContext);
            if (_presenter == null) return;
            await _presenter.PlayAsync(events);
        }

        #endregion

        #region 阶段事件统一播放：供 BattlePhase 在 PerformAll 之外播放流程级事件，presenter 为空时跳过
        public UniTask PlayAsync(IReadOnlyList<IBattleEvent> events_)
            => _presenter == null ? UniTask.CompletedTask : _presenter.PlayAsync(events_);
        #endregion
    }
}
