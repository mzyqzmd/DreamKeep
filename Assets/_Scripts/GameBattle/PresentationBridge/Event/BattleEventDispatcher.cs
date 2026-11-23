using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.EngineUtility;

namespace _DreamKeep.GameBattle
{
    /// <summary>战斗事实广播器：表现层与域内反应方（遗物等）共同订阅的下行出口，机械分派由通用
    /// <see cref="EventDispatcher{TEvent}"/> 承担，本类只多一层"顺序播放一批事件"的批量入口。
    /// 各订阅者初始化时自注册关心的处理器，未注册的事件静默忽略；订阅者只能读事件、不许改事件已表达的结算结果。</summary>
    public sealed class BattleEventDispatcher : EventDispatcher<IBattleEvent>
    {
        public BattleEventDispatcher() : base("战斗事件") { }

        /// <summary>顺序播放一批事件：逐条 await，一条事件内部的各处理器并行，未注册的事件跳过。</summary>
        public async UniTask PlayAsync(IReadOnlyList<IBattleEvent> events_)
        {
            for (int i = 0; i < events_.Count; i++)
                await base.PlayAsync(events_[i]);
        }
    }
}
