using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.GameBattle
{
    /// <summary>动作系统：动作队列 + 异步串行排空，每个动作执行完后等待表现层演出完成再进下一个。</summary>
    public class ActionSystem
    {
        private readonly Queue<BattleAction> _queue = new();
        private readonly BattleContext _battleContext;
        private readonly BattleEventDispatcher _eventDispatcher;
        private bool _performing; // 排空独占：重复调用在首个 await 之前同步命中守卫后返回

        public ActionSystem(BattleContext battleContext_, BattleEventDispatcher eventDispatcher_)
        {
            _battleContext = battleContext_;
            _eventDispatcher = eventDispatcher_;
        }

        #region 入队一个动作，队列不设上限且永不丢动作

        public void Enqueue(BattleAction action_) => _queue.Enqueue(action_);

        #endregion

        #region 异步串行排空：每动作执行后 await 表现层播放，Post 钩子追加的动作会在本轮继续出队

        public async UniTask PerformAllAsync()
        {
            if (_performing) return;
            _performing = true;
            try
            {
                while (_queue.Count > 0)
                {
                    List<IBattleEvent> events = _queue.Dequeue().Execute(_battleContext);
                    await _eventDispatcher.PlayAsync(events);
                }
            }
            finally
            {
                // 异常也必须复位：调用方是 Forget，异常不上抛，守卫漏复位会让此后所有排空同步空转、战斗静默卡死
                _performing = false;
            }
        }

        #endregion
    }
}