using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.GameRun
{
    /// <summary>事件段事实广播器：本段规则层与表现视图共用的下行出口，持有类型→处理器列表字典，按事件类型查表分派；
    /// 同一类型可被多处订阅、同条事件内并行 await（一处演出同时开播、等到最长的那个），未注册的事件静默忽略。
    /// 实例随事件段 scope 创建与销毁，不与其它段共享，段卸载后订阅不会残留。</summary>
    public class EventEventDispatcher
    {
        private readonly Dictionary<Type, List<Func<IEventSegmentEvent, UniTask>>> _handlers = new();

        /// <summary>追加注册某类事件的处理器：同类型多视图各自订阅互不覆盖，同条事件内并行分派。</summary>
        public void Register<TEvent>(Func<TEvent, UniTask> handler_) where TEvent : IEventSegmentEvent
        {
            Type type = typeof(TEvent);
            if (!_handlers.TryGetValue(type, out List<Func<IEventSegmentEvent, UniTask>> handlers))
                _handlers[type] = handlers = new List<Func<IEventSegmentEvent, UniTask>>();
            handlers.Add(e => handler_((TEvent)e));
        }

        /// <summary>注册不等待演出的处理器：事件链不会因它停顿时，用这个重载，处理器不必伪装成返回 UniTask 的异步方法。</summary>
        public void Register<TEvent>(Action<TEvent> handler_) where TEvent : IEventSegmentEvent =>
            Register<TEvent>(e =>
            {
                handler_(e);
                return UniTask.CompletedTask;
            });

        /// <summary>播放单条事实：逐处理器并行等待，未注册的类型静默跳过。</summary>
        public UniTask PlayAsync(IEventSegmentEvent event_) => DispatchAsync(event_);

        // 单条分派：同一条事件的各处理器同时起步、并行等待；
        // 并行等待用局部列表收集，防止处理器嵌套分派时复用字段清掉外层尚未等待完的任务
        private async UniTask DispatchAsync(IEventSegmentEvent event_)
        {
            if (!_handlers.TryGetValue(event_.GetType(), out List<Func<IEventSegmentEvent, UniTask>> handlers)) return;
            if (handlers.Count == 1)
            {
                await handlers[0](event_);
                return;
            }

            List<UniTask> waits = new(handlers.Count);
            for (int i = 0; i < handlers.Count; i++) waits.Add(handlers[i](event_));
            await UniTask.WhenAll(waits);
        }
    }
}
