namespace _DreamKeep.ConfigData
{
    /// <summary>状态结算时机：一律按宿主（挂载该状态的生物）自己的回合边界计；结算一次 = 先触发效果，再让层数递减一层
    /// （层数变化与是否移除可由状态覆写 Settle 自定义）。</summary>
    public enum EPerkSettleTiming
    {
        /// <summary>不绑定回合边界：不产触发动作、层数永不递减，被动通道（数值修正等）照常生效。</summary>
        None,

        /// <summary>宿主回合开始时结算一次。</summary>
        TurnStart,

        /// <summary>宿主回合结束时结算一次。</summary>
        TurnEnd,
    }
}
