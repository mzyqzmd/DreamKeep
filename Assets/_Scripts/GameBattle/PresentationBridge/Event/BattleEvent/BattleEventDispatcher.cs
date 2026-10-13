using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.GameBattle
{
    /// <summary>事件分发器：表现层唯一的下行出口，持有类型→处理器列表字典，按事件类型查表 await 顺序分派；各视图初始化时自注册关心的处理器，同一类型可被多个视图订阅、按注册顺序依次 await，未注册的事件静默忽略。</summary>
    public class BattleEventDispatcher
    {
        private readonly Dictionary<Type, List<Func<IBattleEvent, UniTask>>> _handlers = new();

        /// <summary>追加注册某类事件的处理器：同类型多视图各自订阅互不覆盖，注册顺序即分派顺序。</summary>
        public void Register<TEvent>(Func<TEvent, UniTask> handler_) where TEvent : IBattleEvent
        {
            Type type = typeof(TEvent);
            if (!_handlers.TryGetValue(type, out List<Func<IBattleEvent, UniTask>> handlers))
                _handlers[type] = handlers = new List<Func<IBattleEvent, UniTask>>();
            handlers.Add(e => handler_((TEvent)e));
        }

        /// <summary>顺序播放一批事件：逐条 await 该类型的全部处理器，未注册的事件跳过。</summary>
        public async UniTask PlayAsync(IReadOnlyList<IBattleEvent> events_)
        {
            for (int i = 0; i < events_.Count; i++)
                await DispatchAsync(events_[i]);
        }

        /// <summary>播放单条事件：阶段层只产出这一条事件时直接走它，免去为它包一个列表。</summary>
        public UniTask PlayAsync(IBattleEvent battleEvent_) => DispatchAsync(battleEvent_);

        // 单条分派：索引遍历，分派期不增删处理器（注册只发生在注入期），无需快照拷贝
        private async UniTask DispatchAsync(IBattleEvent battleEvent_)
        {
            if (!_handlers.TryGetValue(battleEvent_.GetType(), out List<Func<IBattleEvent, UniTask>> handlers)) return;
            for (int i = 0; i < handlers.Count; i++)
                await handlers[i](battleEvent_);
        }
    }
}
