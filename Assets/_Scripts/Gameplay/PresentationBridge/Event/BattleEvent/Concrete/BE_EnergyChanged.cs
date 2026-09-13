namespace _DreamKeep.Gameplay
{
    /// <summary>能量变化：打出消耗、获得能量或回合回能后写出当前值与上限。</summary>
    public struct BE_EnergyChanged : IBattleEvent
    {
        public int Current { get; }
        public int Max { get; }

        public BE_EnergyChanged(int current_, int max_)
        {
            Current = current_;
            Max = max_;
        }
    }
}
