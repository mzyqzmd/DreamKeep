using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.GameBattle
{
    /// <summary>战斗事实广播器：表现层与域内反应方（遗物等）共同订阅的下行出口，持有类型→处理器列表字典，按事件类型查表分派；
    /// 各订阅者初始化时自注册关心的处理器，同一类型可被多处订阅、同条事件内并行 await（一波演出同时开播、等到最长的那个），
    /// 未注册的事件静默忽略。订阅者只能读事件、不许改事件已表达的结算结果。
    /// 与 GameRun 的 SegmentEventDispatcher 是同构实现（Register 两个重载 + DispatchAsync 逐字相同），
    /// 差异只在基类型用 IBattleEvent、且有批量播放与注册数查询；两份刻意不合并（GameBattle 不能反引 GameRun），改一处必改另一处。</summary>
    public class BattleEventDispatcher
    {
        private readonly Dictionary<Type, List<Func<IBattleEvent, UniTask>>> _handlers = new();

        /// <summary>追加注册某类事件的处理器：同类型多视图各自订阅互不覆盖，同条事件内并行分派。</summary>
        public void Register<TEvent>(Func<TEvent, UniTask> handler_) where TEvent : IBattleEvent
        {
            Type type = typeof(TEvent);
            if (!_handlers.TryGetValue(type, out List<Func<IBattleEvent, UniTask>> handlers))
                _handlers[type] = handlers = new List<Func<IBattleEvent, UniTask>>();
            handlers.Add(e => handler_((TEvent)e));
        }

        /// <summary>注册不等待演出的处理器：事件链不会因它停顿时，用这个重载，处理器不必伪装成返回 UniTask 的异步方法。</summary>
        public void Register<TEvent>(Action<TEvent> handler_) where TEvent : IBattleEvent =>
            Register<TEvent>(e =>
            {
                handler_(e);
                return UniTask.CompletedTask;
            });

        /// <summary>装配期断言用：某事件类型的处理器注册数。只读查询，不改分派行为。</summary>
        public int CountHandlers<TEvent>() where TEvent : IBattleEvent =>
            _handlers.TryGetValue(typeof(TEvent), out List<Func<IBattleEvent, UniTask>> handlers) ? handlers.Count : 0;

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

            // 并行等待用局部列表收集：处理器会在分派链内嵌套播放新事件（遗物效果在事件处理器里执行动作），
            // 复用字段会被嵌套分派 Clear 掉外层尚未等待完的任务，故每次分派各自收集
            List<UniTask> waits = new(handlers.Count);
            for (int i = 0; i < handlers.Count; i++)
                waits.Add(handlers[i](battleEvent_));

            await UniTask.WhenAll(waits);
        }
    }
}
