namespace _DreamKeep.Gameplay
{
    /// <summary>能量变化：打出消耗或获得能量后写出当前值。</summary>
    public class EnergyChangedEvent : IDomainEvent
    {
        public int Current { get; }

        public EnergyChangedEvent(int current_) => Current = current_;
    }
}
