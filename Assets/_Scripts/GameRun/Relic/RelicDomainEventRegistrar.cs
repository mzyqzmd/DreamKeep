using System;
using Cysharp.Threading.Tasks;
using _DreamKeep.GameBattle;

namespace _DreamKeep.GameRun
{
    /// <summary>域侧事件注册门面：把遗物等域内反应方的订阅统一转交严格通道，异常上抛到域循环，
    /// 与表现层演出的隔离通道分开。遗物只认 Dispatcher.Register，因此门面是域侧改严格通道的唯一切换点。</summary>
    public sealed class RelicDomainEventRegistrar
    {
        private readonly BattleEventDispatcher _dispatcher;

        public RelicDomainEventRegistrar(BattleEventDispatcher dispatcher_) => _dispatcher = dispatcher_;

        #region 注册：两个重载与通用分派器对齐，转发到严格通道

        /// <summary>异步处理器注册：转交严格通道，异常不再被吞。</summary>
        public void Register<TConcrete>(Func<TConcrete, UniTask> handler_) where TConcrete : IBattleEvent =>
            _dispatcher.RegisterStrict(handler_);

        /// <summary>同步处理器注册：转交严格通道，异常不再被吞。</summary>
        public void Register<TConcrete>(Action<TConcrete> handler_) where TConcrete : IBattleEvent =>
            _dispatcher.RegisterStrict(handler_);

        #endregion
    }
}
