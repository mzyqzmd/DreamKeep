using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.GameBattle
{
    /// <summary>事件分发器：表现层唯一的下行出口，持有类型→处理器列表字典，按事件类型查表分派；各视图初始化时自注册关心的处理器，
    /// 同一类型可被多个视图订阅、同条事件内并行 await（一波演出同时开播、等到最长的那个），未注册的事件静默忽略。</summary>
    public class BattleEventDispatcher
    {
        private readonly Dictionary<Type, List<Func<IBattleEvent, UniTask>>> _handlers = new();
        private readonly List<UniTask> _waits = new(); // 并行等待的复用容器

        /// <summary>追加注册某类事件的处理器：同类型多视图各自订阅互不覆盖，同条事件内并行分派。</summary>
        public void Register<TEvent>(Func<TEvent, UniTask> handler_) where TEvent : IBattleEvent
        {
            Type type = typeof(TEvent);
            if (!_handlers.TryGetValue(type, out List<Func<IBattleEvent, UniTask>> handlers))
                _handlers[type] = handlers = new List<Func<IBattleEvent, UniTask>>();
            handlers.Add(e => handler_((TEvent)e));
        }

        /// <summary>顺序播放一批事件：逐条 await，一条事件内部的各处理器并行，未注册的事件跳过。</summary>
        public async UniTask PlayAsync(IReadOnlyList<IBattleEvent> events_)
        {
            for (int i = 0; i < events_.Count; i++)
                await DispatchAsync(events_[i]);
        }

        /// <summary>播放单条事件：阶段层只产出这一条事件时直接走它，免去为它包一个列表。</summary>
        public UniTask PlayAsync(IBattleEvent battleEvent_) => DispatchAsync(battleEvent_);

        // 单条分派：同一条事件的各处理器同时起步、并行等待（一波演出同时开播，等到最长的那个）；
        // 分派期不增删处理器（注册只发生在注入期），无需快照拷贝
        private async UniTask DispatchAsync(IBattleEvent battleEvent_)
        {
            if (!_handlers.TryGetValue(battleEvent_.GetType(), out List<Func<IBattleEvent, UniTask>> handlers)) return;
            if (handlers.Count == 1)
            {
                await handlers[0](battleEvent_);
                return;
            }

            _waits.Clear();
            for (int i = 0; i < handlers.Count; i++)
                _waits.Add(handlers[i](battleEvent_));

            await UniTask.WhenAll(_waits);
            _waits.Clear();
        }
    }
}
