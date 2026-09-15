using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.GameBattle
{
    /// <summary>事件分发器：表现层唯一的下行出口，持有类型→处理器字典，按事件类型查表 await 顺序分派；各视图初始化时自注册关心的处理器，未注册的事件静默忽略。</summary>
    public class BattleEventDispatcher
    {
        private readonly Dictionary<Type, Func<IBattleEvent, UniTask>> _handlers = new();

        /// <summary>注册某类事件的处理器；同类型重复注册会覆盖。</summary>
        public void Register<TEvent>(Func<TEvent, UniTask> handler_) where TEvent : IBattleEvent
            => _handlers[typeof(TEvent)] = e => handler_((TEvent)e);

        public async UniTask PlayAsync(IReadOnlyList<IBattleEvent> events_)
        {
            foreach (IBattleEvent e in events_)
                if (_handlers.TryGetValue(e.GetType(), out Func<IBattleEvent, UniTask> handler))
                    await handler(e);
        }
    }
}