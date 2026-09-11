using System.Collections.Generic;
using _DreamKeep.Gameplay;
using VContainer.Unity;

namespace _DreamKeep.Presentation
{
    /// <summary>事件泵：把战斗产出的领域事件按帧泵给手牌视图与播放队列，是表现层唯一的事件消费入口。</summary>
    public class BattleEventPump : IStartable, ITickable
    {
        private readonly BattleLoop _loop;
        private readonly Obj_HandView _handView;
        private readonly BattlePlaybackQueue _playbackQueue;

        public BattleEventPump(BattleLoop loop_, Obj_HandView handView_, BattlePlaybackQueue playbackQueue_)
        {
            _loop = loop_;
            _handView = handView_;
            _playbackQueue = playbackQueue_;
        }

        #region 开战：视图与播放队列已由容器注入完毕，此时启动才不会丢首批事件
        public void Start() => _loop.Start();
        #endregion

        #region 每帧抽干事件缓冲并分发，缓冲为空时直接返回
        public void Tick()
        {
            if (_loop.Events.Count == 0) return;
            ConsumeEvents(_loop.Events.Drain());
        }
        #endregion

        #region 消费一批领域事件：抽牌生成卡面，离场回收卡面，末尾统一重排后交给播放队列
        private void ConsumeEvents(List<IBattleEvent> events_)
        {
            for (int i = 0; i < events_.Count; i++)
            {
                switch (events_[i])
                {
                    case IBE_CardDrawn drawn:
                        _handView.Spawn(drawn.Card);
                        break;
                    case IBE_CardDiscarded discarded:
                        _handView.Despawn(discarded.Card);
                        break;
                    case IBE_CardExhausted exhausted:
                        _handView.Despawn(exhausted.Card);
                        break;
                    case IBE_CardErased erased:
                        _handView.Despawn(erased.Card);
                        break;
                }
            }
            _handView.Relayout();
            _playbackQueue.Enqueue(events_);
        }
        #endregion
    }
}
