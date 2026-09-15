using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>血量恢复：再生等治疗来源，供表现层与日志消费者观测。</summary>
    public struct BE_HealthRestored : IBattleEvent
    {
        public Creature Target { get; }
        public int Amount { get; }

        public BE_HealthRestored(Creature target_, int amount_)
        {
            Target = target_;
            Amount = amount_;
        }
    }
}
