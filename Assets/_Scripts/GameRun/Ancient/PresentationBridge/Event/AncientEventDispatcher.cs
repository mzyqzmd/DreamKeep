using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.GameRun
{
    /// <summary>先古之民段事实广播器：本段规则层与表现视图共用的下行出口，持有类型→处理器列表字典，按事件类型查表分派；
    /// 同一类型可被多处订阅、同条事件内并行 await，未注册的事件静默忽略。
    /// 实例随先古段 scope 创建与销毁，不与其它段共享，段卸载后订阅不会残留。</summary>
    public class AncientEventDispatcher
    {
        private readonly Dictionary<Type, List<Func<IAncientEvent, UniTask>>> _handlers = new();

        /// <summary>追加注册某类事件的处理器：同类型多视图各自订阅互不覆盖，同条事件内并行分派。</summary>
        public void Register<TEvent>(Func<TEvent, UniTask> handler_) where TEvent : IAncientEvent
        {
            Type type = typeof(TEvent);
            if (!_handlers.TryGetValue(type, out List<Func<IAncientEvent, UniTask>> handlers))
                _handlers[type] = handlers = new List<Func<IAncientEvent, UniTask>>();
            handlers.Add(e => handler_((TEvent)e));
        }

        /// <summary>注册不等待演出的处理器：事件链不会因它停顿时，用这个重载。</summary>
        public void Register<TEvent>(Action<TEvent> handler_) where TEvent : IAncientEvent =>
            Register<TEvent>(e =>
            {
                handler_(e);
                return UniTask.CompletedTask;
            });

        /// <summary>播放单条事实：逐处理器并行等待，未注册的类型静默跳过。</summary>
        public UniTask PlayAsync(IAncientEvent ancientEvent_) => DispatchAsync(ancientEvent_);

        private async UniTask DispatchAsync(IAncientEvent ancientEvent_)
        {
            if (!_handlers.TryGetValue(ancientEvent_.GetType(), out List<Func<IAncientEvent, UniTask>> handlers)) return;
            if (handlers.Count == 1)
            {
                await handlers[0](ancientEvent_);
                return;
            }

            List<UniTask> waits = new(handlers.Count);
            for (int i = 0; i < handlers.Count; i++) waits.Add(handlers[i](ancientEvent_));
            await UniTask.WhenAll(waits);
        }
    }
}
