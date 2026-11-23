using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _DreamKeep.EngineUtility
{
    /// <summary>通用事实广播器：持有类型→处理器列表字典，按事件类型查表分派；同一类型可被多处订阅、
    /// 同条事件内并行 await（一波演出同时开播、等到最长的那个），未注册的事件静默忽略。
    /// 两条注册通道：Register 给表现层，处理器异常只记日志继续演出；RegisterStrict 给域侧，异常上抛由调用方处置。
    /// 标签由构造注入（如"战斗事件"/"段事件"），日志文案随调用方语义，本类不认识任何业务类型。</summary>
    public class EventDispatcher<TEvent>
    {
        private readonly string _label;
        private readonly Dictionary<Type, List<(Func<TEvent, UniTask> Handler, bool Strict)>> _handlers = new();

        /// <summary>注入日志标签：仅用于隔离通道的处理器异常日志前缀。</summary>
        public EventDispatcher(string label_) => _label = label_;

        #region 注册：表现层走隔离通道，域侧走严格上抛通道

        /// <summary>表现层注册：处理器异常只记日志，不影响同批其他演出与后续结算。</summary>
        public void Register<TConcrete>(Func<TConcrete, UniTask> handler_) where TConcrete : TEvent =>
            Add<TConcrete>(handler_, false);

        /// <summary>表现层注册（不等待演出）：事件链不会因它停顿时用这个重载，处理器不必伪装成返回 UniTask。</summary>
        public void Register<TConcrete>(Action<TConcrete> handler_) where TConcrete : TEvent =>
            Add<TConcrete>(e => { handler_(e); return UniTask.CompletedTask; }, false);

        /// <summary>域侧注册：处理器异常上抛到调用方，不再被静默隔离。</summary>
        public void RegisterStrict<TConcrete>(Func<TConcrete, UniTask> handler_) where TConcrete : TEvent =>
            Add<TConcrete>(handler_, true);

        /// <summary>域侧注册（不等待演出）：异常同样上抛。</summary>
        public void RegisterStrict<TConcrete>(Action<TConcrete> handler_) where TConcrete : TEvent =>
            Add<TConcrete>(e => { handler_(e); return UniTask.CompletedTask; }, true);

        #endregion

        /// <summary>播放单条事件：逐处理器并行等待，未注册的类型静默跳过。</summary>
        public UniTask PlayAsync(TEvent event_) => DispatchAsync(event_);

        // 追加处理器：同类型多视图互不覆盖，条目自带异常策略，分派时按条目决定隔离或上抛
        private void Add<TConcrete>(Func<TConcrete, UniTask> handler_, bool strict_) where TConcrete : TEvent
        {
            Type type = typeof(TConcrete);
            if (!_handlers.TryGetValue(type, out List<(Func<TEvent, UniTask> Handler, bool Strict)> handlers))
                _handlers[type] = handlers = new();
            handlers.Add((e => handler_((TConcrete)e), strict_));
        }

        // 单条分派：各处理器同时起步、并行等待；
        // 并行等待列表每次分派局部创建——处理器会在分派链内嵌套播放新事件（遗物效果、续接演出），
        // 复用字段会被嵌套分派 Clear 掉外层尚未等待完的任务。
        private async UniTask DispatchAsync(TEvent event_)
        {
            if (!_handlers.TryGetValue(event_.GetType(), out List<(Func<TEvent, UniTask> Handler, bool Strict)> handlers)) return;
            if (handlers.Count == 1)
            {
                await Invoke(handlers[0], event_);
                return;
            }

            List<UniTask> waits = new(handlers.Count);
            for (int i = 0; i < handlers.Count; i++)
                waits.Add(Invoke(handlers[i], event_));
            await UniTask.WhenAll(waits);
        }

        // 单处理器边界：严格条目直接 await（异常上抛），隔离条目吞异常只记日志
        private UniTask Invoke((Func<TEvent, UniTask> Handler, bool Strict) entry_, TEvent event_) =>
            entry_.Strict ? entry_.Handler(event_) : InvokeSafely(entry_.Handler, event_);

        // 隔离通道：演出异常只记日志不上抛——一条动画报错不该终止域动作链（队列会留下半截动作、后续命令错序）。
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
