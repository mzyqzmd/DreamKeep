namespace _DreamKeep.GameRun
{
    /// <summary>整局通关：Boss 节点战斗胜利即通关（Boss 恒为最后一行，不再有后续层），表现侧借此弹整局结果面板，
    /// 确认后走 ReturnToMenu 回主菜单重开；跳过奖励链。</summary>
    public struct BAE_RunCleared : IBattleSegmentEvent
    {
    }
}
