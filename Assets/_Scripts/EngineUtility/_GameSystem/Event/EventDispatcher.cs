using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _DreamKeep.EngineUtility
{
    /// <summary>通用事实广播器：持有类型→处理器列表字典，按事件类型查表分派；同一类型可被多处订阅、
    /// 同条事件内并行 await（一波演出同时开播、等到最长的那个），未注册的事件静默忽略。
    /// 订阅者只能读事件、不许改事件已表达的结算结果。处理器异常只记日志不上抛——一条演出报错不该终止域动作链。
    /// 标签由构造注入（如"战斗事件"/"段事件"），日志文案随调用方语义，本类不认识任何业务类型。</summary>
    public class EventDispatcher<TEvent>
    {
        private readonly string _label;
        private readonly Dictionary<Type, List<Func<TEvent, UniTask>>> _handlers = new();

        /// <summary>注入日志标签：仅用于处理器异常时的日志前缀。</summary>
        public EventDispatcher(string label_) => _label = label_;

        /// <summary>追加注册某类事件的处理器：同类型多视图各自订阅互不覆盖，同条事件内并行分派。</summary>
        public void Register<TConcrete>(Func<TConcrete, UniTask> handler_) where TConcrete : TEvent
        {
            Type type = typeof(TConcrete);
            if (!_handlers.TryGetValue(type, out List<Func<TEvent, UniTask>> handlers))
                _handlers[type] = handlers = new List<Func<TEvent, UniTask>>();
            handlers.Add(e => handler_((TConcrete)e));
        }

        /// <summary>注册不等待演出的处理器：事件链不会因它停顿时用这个重载，处理器不必伪装成返回 UniTask。</summary>
        public void Register<TConcrete>(Action<TConcrete> handler_) where TConcrete : TEvent =>
            Register<TConcrete>(e =>
            {
                handler_(e);
                return UniTask.CompletedTask;
            });

        /// <summary>播放单条事件：逐处理器并行等待，未注册的类型静默跳过。</summary>
        public UniTask PlayAsync(TEvent event_) => DispatchAsync(event_);

        // 单条分派：各处理器同时起步、并行等待；
        // 并行等待列表每次分派局部创建——处理器会在分派链内嵌套播放新事件（遗物效果、续接演出），
        // 复用字段会被嵌套分派 Clear 掉外层尚未等待完的任务。
        private async UniTask DispatchAsync(TEvent event_)
        {
            if (!_handlers.TryGetValue(event_.GetType(), out List<Func<TEvent, UniTask>> handlers)) return;
            if (handlers.Count == 1)
            {
                await InvokeSafely(handlers[0], event_);
                return;
            }

            List<UniTask> waits = new(handlers.Count);
            for (int i = 0; i < handlers.Count; i++)
                waits.Add(InvokeSafely(handlers[i], event_));
            await UniTask.WhenAll(waits);
        }

        // 单处理器边界：演出异常只记日志不上抛——一条动画报错不该终止域动作链（队列会留下半截动作、后续命令错序）。
        // 日志带事件类型，排查先搜这里。
        private async UniTask InvokeSafely(Func<TEvent, UniTask> handler_, TEvent event_)
        {
            try
            {
                await handler_(event_);
            }
            catch (Exception exception)
            {
                Debug.LogError($"{_label} {event_.GetType().Name} 的处理器抛出异常，已隔离并继续后续结算");
                Debug.LogException(exception);
            }
        }
    }
}
