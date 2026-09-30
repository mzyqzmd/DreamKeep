namespace _DreamKeep.ConfigData
{
    /// <summary>状态衰减时机：一律按宿主（挂载该状态的生物）自己的回合边界计。</summary>
    public enum EPerkDecayTiming
    {
        /// <summary>永不自动衰减：力量等永久状态。</summary>
        None,

        /// <summary>宿主回合开始时递减一层。</summary>
        TurnStart,

        /// <summary>宿主回合结束时递减一层。</summary>
        TurnEnd,
    }
}
