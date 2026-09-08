namespace _DreamKeep.Gameplay
{
    /// <summary>格挡获得：记录目标与获得的格挡值。</summary>
    public class BlockGainedEvent : IDomainEvent
    {
        public Actor Target { get; }
        public int Amount { get; }

        public BlockGainedEvent(Actor target_, int amount_)
        {
            Target = target_;
            Amount = amount_;
        }
    }
}
