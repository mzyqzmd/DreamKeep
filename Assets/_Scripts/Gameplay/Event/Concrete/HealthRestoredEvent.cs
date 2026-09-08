namespace _DreamKeep.Gameplay
{
    /// <summary>血量恢复：再生等治疗来源，供表现层与日志消费者观测。</summary>
    public class HealthRestoredEvent : IDomainEvent
    {
        public Actor Target { get; }
        public int Amount { get; }

        public HealthRestoredEvent(Actor target_, int amount_)
        {
            Target = target_;
            Amount = amount_;
        }
    }
}
