using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.GameBattle
{
    /// <summary>动作系统：动作队列 + 异步串行排空，每个动作执行完后等待表现层演出完成再进下一个。</summary>
    public class ActionSystem
    {
        private readonly Queue<BattleAction> _queue = new();
        private readonly BattleContext _battleContext;
        private readonly BattleEventDispatcher _dispatcher;
        private bool _performing; // 排空独占：重复调用在首个 await 之前同步命中守卫后返回

        public ActionSystem(BattleContext battleContext_, BattleEventDispatcher dispatcher_)
        {
            _battleContext = battleContext_;
            _dispatcher = dispatcher_;
        }

        #region 入队一个动作，队列不设上限且永不丢动作

        public void Enqueue(BattleAction action_) => _queue.Enqueue(action_);

        #endregion

        #region 异步串行排空：每动作执行后 await 表现层播放，Post 钩子追加的动作会在本轮继续出队

        public async UniTask PerformAllAsync()
        {
            if (_performing) return;
            _performing = true;
            while (_queue.Count > 0)
            {
                List<IBattleEvent> events = _queue.Dequeue().Execute(_battleContext);
                await _dispatcher.PlayAsync(events);
            }

            _performing = false;
        }

        #endregion

        #region 阶段事件统一播放：供 BattlePhase 在 PerformAll 之外播放流程级事件

        public UniTask PlayAsync(IBattleEvent battleEvent_) => _dispatcher.PlayAsync(battleEvent_);

        #endregion
    }
}
