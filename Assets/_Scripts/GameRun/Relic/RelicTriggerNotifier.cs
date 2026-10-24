using System;
using _DreamKeep.ConfigData;

namespace _DreamKeep.GameRun
{
    /// <summary>遗物播报器：遗物效果生效与用尽时同步广播是哪一件遗物，表现层据此点亮槽位或置灰。
    /// 不走战斗事件总线——遗物是整局物件，跑图段没有战斗分发器时同样要有出口；订阅者与战斗容器同生共死，不注销。</summary>
    public sealed class RelicTriggerNotifier
    {
        public event Action<SO_Relic> Triggered;
        public event Action<SO_Relic> Exhausted;

        /// <summary>本件遗物刚生效：涟漪与动作演出同帧开始。</summary>
        public void NotifyTriggered(SO_Relic relic_) => Triggered?.Invoke(relic_);

        /// <summary>本件遗物已用尽：表现层把槽位置灰。</summary>
        public void NotifyExhausted(SO_Relic relic_) => Exhausted?.Invoke(relic_);
    }
}
