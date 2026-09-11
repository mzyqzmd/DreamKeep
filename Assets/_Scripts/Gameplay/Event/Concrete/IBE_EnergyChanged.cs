namespace _DreamKeep.Gameplay
{
    /// <summary>能量变化：打出消耗或获得能量后写出当前值。</summary>
    public struct IBE_EnergyChanged : IBattleEvent
    {
        public int Current { get; }

        public IBE_EnergyChanged(int current_) => Current = current_;
    }
}
